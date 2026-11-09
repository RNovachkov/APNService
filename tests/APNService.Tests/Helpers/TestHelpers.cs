using System.Net;
using System.Security.Cryptography;
using APNCore.Auth;

namespace APNService.Tests.Helpers;

/// <summary>A <see cref="TimeProvider"/> whose current time can be set by tests.</summary>
internal sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan delta) => Now += delta;
}

/// <summary>Captures outgoing requests and returns canned responses, with no network I/O.</summary>
internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, string?, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly Lock _sync = new();

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string?> Bodies { get; } = [];

    public HttpRequestMessage LastRequest => Requests[^1];
    public string? LastBody => Bodies[^1];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        lock (_sync)
        {
            Requests.Add(request);
            Bodies.Add(body);
        }

        return responder(request, body);
    }

    public static HttpResponseMessage Ok(string? apnsId = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        if (apnsId is not null)
            response.Headers.TryAddWithoutValidation("apns-id", apnsId);
        return response;
    }

    public static HttpResponseMessage Error(HttpStatusCode status, string body)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body),
        };
    }
}

/// <summary>Factory for building a provider backed by a freshly generated P-256 key.</summary>
internal static class TestKeys
{
    public static (ApnJwtProvider Provider, ECDsa KeyPair) NewProvider(
        string teamId = "TEAM123456",
        string keyId = "KEY1234567",
        TimeProvider? timeProvider = null)
    {
        var keyPair = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pem = keyPair.ExportPkcs8PrivateKeyPem();

        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(pem);
        var provider = new ApnJwtProvider(teamId, keyId, ecdsa, timeProvider);

        // keyPair is returned so tests can verify signatures with the matching public key.
        return (provider, keyPair);
    }

    public static string NewPem()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return key.ExportPkcs8PrivateKeyPem();
    }
}
