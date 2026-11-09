using System.Net;
using System.Text;
using System.Text.Json;
using APNCore;
using APNCore.Models;

namespace APNService;

/// <summary>
/// A client for the APNs HTTP/2 provider API. It signs each request with a provider token generated
/// from your <c>.p8</c> key and can deliver every APNs push type.
/// </summary>
/// <remarks>
/// The client is thread-safe and designed to be long-lived: create one per environment and reuse it
/// so the HTTP/2 connection and the cached provider token are shared across sends.
/// </remarks>
public sealed class ApnClient : IApnClient, IDisposable
{
    private readonly ApnClientOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates a client that owns its own HTTP/2 <see cref="HttpClient"/>.</summary>
    /// <param name="options">The client configuration.</param>
    public ApnClient(ApnClientOptions options)
        : this(options, CreateDefaultHttpClient(options), ownsHttpClient: true)
    {
    }

    /// <summary>
    /// Creates a client using a caller-supplied <see cref="HttpClient"/> (for example one from
    /// <c>IHttpClientFactory</c>). The caller is responsible for configuring HTTP/2 support and the
    /// base address, and for disposing the client.
    /// </summary>
    /// <param name="options">The client configuration.</param>
    /// <param name="httpClient">A pre-configured HTTP client.</param>
    /// <param name="ownsHttpClient">When <see langword="true"/>, the <see cref="HttpClient"/> is disposed with this instance.</param>
    public ApnClient(ApnClientOptions options, HttpClient httpClient, bool ownsHttpClient = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.JwtProvider);
        ArgumentNullException.ThrowIfNull(httpClient);

        _options = options;
        _http = httpClient;
        _ownsHttpClient = ownsHttpClient;

        if (_http.BaseAddress is null)
            _http.BaseAddress = options.Environment.GetBaseUri(options.UseAlternatePort);
    }

    private static HttpClient CreateDefaultHttpClient(ApnClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var handler = new SocketsHttpHandler
        {
            // APNs supports many concurrent streams; allow more than one connection for throughput.
            EnableMultipleHttp2Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = options.Environment.GetBaseUri(options.UseAlternatePort),
            Timeout = options.Timeout,
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };
    }

    /// <summary>
    /// Sends a single notification to APNs and returns the result. This method does not throw for
    /// APNs-level rejections (invalid token, etc.); inspect <see cref="ApnResponse.IsSuccess"/> or
    /// call <see cref="ApnResponse.EnsureSuccess"/>.
    /// </summary>
    /// <param name="notification">The notification to deliver.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task<ApnResponse> SendAsync(ApnNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentException.ThrowIfNullOrWhiteSpace(notification.DeviceToken);

        var topic = notification.Topic ?? _options.DefaultTopic
            ?? throw new InvalidOperationException(
                "No apns-topic was supplied: set ApnNotification.Topic or ApnClientOptions.DefaultTopic.");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/3/device/{notification.DeviceToken}")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        request.Headers.TryAddWithoutValidation("authorization", $"bearer {_options.JwtProvider.GetToken()}");
        request.Headers.TryAddWithoutValidation("apns-topic", topic);
        request.Headers.TryAddWithoutValidation("apns-push-type", notification.PushType.ToHeaderValue());

        if (notification.Priority is { } priority)
            request.Headers.TryAddWithoutValidation("apns-priority", ((int)priority).ToString());

        if (notification.Expiration is { } expiration)
            request.Headers.TryAddWithoutValidation("apns-expiration", expiration.ToUnixTimeSeconds().ToString());

        if (!string.IsNullOrEmpty(notification.CollapseId))
            request.Headers.TryAddWithoutValidation("apns-collapse-id", notification.CollapseId);

        if (!string.IsNullOrEmpty(notification.Id))
            request.Headers.TryAddWithoutValidation("apns-id", notification.Id);

        var json = notification.Payload.ToJson(_options.JsonSerializerOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ParseResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends many notifications concurrently, returning a result for each in the same order as the
    /// input. Failures are reported per-notification rather than throwing.
    /// </summary>
    /// <param name="notifications">The notifications to deliver.</param>
    /// <param name="maxConcurrency">The maximum number of in-flight requests. Defaults to 10.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<IReadOnlyList<ApnResponse>> SendManyAsync(
        IReadOnlyList<ApnNotification> notifications,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        var results = new ApnResponse[notifications.Count];
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = new Task[notifications.Count];
        for (var i = 0; i < notifications.Count; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    results[index] = await SendAsync(notifications[index], cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    gate.Release();
                }
            }, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    private static async Task<ApnResponse> ParseResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var apnsId = GetHeader(response, "apns-id");
        var apnsUniqueId = GetHeader(response, "apns-unique-id");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return new ApnResponse
            {
                IsSuccess = true,
                StatusCode = statusCode,
                ApnsId = apnsId,
                ApnsUniqueId = apnsUniqueId,
            };
        }

        string? reason = null;
        DateTimeOffset? timestamp = null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.TryGetProperty("reason", out var reasonElement))
                    reason = reasonElement.GetString();

                if (root.TryGetProperty("timestamp", out var timestampElement) &&
                    timestampElement.TryGetInt64(out var unixMilliseconds))
                {
                    timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
                }
            }
            catch (JsonException)
            {
                // Leave reason null if the body is not the expected JSON shape.
            }
        }

        return new ApnResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ApnsId = apnsId,
            ApnsUniqueId = apnsUniqueId,
            Reason = reason,
            Timestamp = timestamp,
        };
    }

    private static string? GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }
}
