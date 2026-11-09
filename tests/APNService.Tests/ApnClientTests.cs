using System.Net;
using System.Text.Json;
using APNService;
using APNCore;
using APNCore.Auth;
using APNCore.Models;
using APNService.Tests.Helpers;
using Xunit;

namespace APNService.Tests;

public sealed class ApnClientTests
{
    private static (ApnClient Client, StubHttpMessageHandler Handler, ApnJwtProvider Provider, IDisposable Key) CreateClient(
        Func<HttpRequestMessage, string?, HttpResponseMessage> responder,
        string? defaultTopic = "com.example.App",
        ApnEnvironment environment = ApnEnvironment.Development)
    {
        var (provider, key) = TestKeys.NewProvider();
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler);

        var client = new ApnClient(
            new ApnClientOptions
            {
                JwtProvider = provider,
                Environment = environment,
                DefaultTopic = defaultTopic,
            },
            http,
            ownsHttpClient: true);

        return (client, handler, provider, key);
    }

    private static string? Header(HttpRequestMessage request, string name) =>
        request.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    [Fact]
    public async Task SendAsync_posts_to_the_device_path_over_http2()
    {
        var (client, handler, provider, key) = CreateClient((_, _) => StubHttpMessageHandler.Ok());
        using (client)
        using (provider)
        using (key)
        {
            await client.SendAsync(ApnNotification.Alert("abc123", "T", "B"));

            var request = handler.LastRequest;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/3/device/abc123", request.RequestUri!.AbsolutePath);
            Assert.Equal(new Version(2, 0), request.Version);
            Assert.Equal("api.sandbox.push.apple.com", request.RequestUri.Host);
        }
    }

    [Fact]
    public async Task SendAsync_sets_the_required_headers()
    {
        var (client, handler, provider, key) = CreateClient((_, _) => StubHttpMessageHandler.Ok());
        using (client)
        using (provider)
        using (key)
        {
            var n = new ApnNotification("tok", ApnPushType.Alert)
            {
                Priority = ApnPriority.High,
                Expiration = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000),
                CollapseId = "group-1",
                Id = "11111111-2222-3333-4444-555555555555",
                Payload = new ApnPayload { Aps = new Aps { AlertBody = "hi" } },
            };

            await client.SendAsync(n);
            var request = handler.LastRequest;

            Assert.StartsWith("bearer ", Header(request, "authorization"));
            Assert.Equal("com.example.App", Header(request, "apns-topic"));
            Assert.Equal("alert", Header(request, "apns-push-type"));
            Assert.Equal("10", Header(request, "apns-priority"));
            Assert.Equal("1700000000", Header(request, "apns-expiration"));
            Assert.Equal("group-1", Header(request, "apns-collapse-id"));
            Assert.Equal("11111111-2222-3333-4444-555555555555", Header(request, "apns-id"));
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        }
    }

    [Fact]
    public async Task SendAsync_omits_optional_headers_when_unset()
    {
        var (client, handler, provider, key) = CreateClient((_, _) => StubHttpMessageHandler.Ok());
        using (client)
        using (provider)
        using (key)
        {
            await client.SendAsync(new ApnNotification("tok", ApnPushType.Background)
            {
                Payload = new ApnPayload { Aps = new Aps { ContentAvailable = 1 } },
            });

            var request = handler.LastRequest;
            Assert.Null(Header(request, "apns-priority"));
            Assert.Null(Header(request, "apns-expiration"));
            Assert.Null(Header(request, "apns-collapse-id"));
            Assert.Null(Header(request, "apns-id"));
        }
    }

    [Fact]
    public async Task SendAsync_uses_notification_topic_over_default()
    {
        var (client, handler, provider, key) = CreateClient((_, _) => StubHttpMessageHandler.Ok());
        using (client)
        using (provider)
        using (key)
        {
            await client.SendAsync(ApnNotification.Voip("tok", "com.example.App.voip"));
            Assert.Equal("com.example.App.voip", Header(handler.LastRequest, "apns-topic"));
        }
    }

    [Fact]
    public async Task SendAsync_throws_when_no_topic_is_available()
    {
        var (client, _, provider, key) = CreateClient((_, _) => StubHttpMessageHandler.Ok(), defaultTopic: null);
        using (client)
        using (provider)
        using (key)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.SendAsync(new ApnNotification("tok", ApnPushType.Alert)));
        }
    }

    [Fact]
    public async Task SendAsync_serializes_the_payload_in_the_body()
    {
        var (client, handler, provider, key) = CreateClient((_, _) => StubHttpMessageHandler.Ok());
        using (client)
        using (provider)
        using (key)
        {
            await client.SendAsync(ApnNotification.Alert("tok", "Hi", "There"));

            using var doc = JsonDocument.Parse(handler.LastBody!);
            var alert = doc.RootElement.GetProperty("aps").GetProperty("alert");
            Assert.Equal("Hi", alert.GetProperty("title").GetString());
            Assert.Equal("There", alert.GetProperty("body").GetString());
        }
    }

    [Fact]
    public async Task SendAsync_returns_success_with_apns_id()
    {
        var (client, _, provider, key) = CreateClient((_, _) => StubHttpMessageHandler.Ok("APNS-ID-1"));
        using (client)
        using (provider)
        using (key)
        {
            var result = await client.SendAsync(ApnNotification.Alert("tok", "T", "B"));

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("APNS-ID-1", result.ApnsId);
            Assert.Null(result.Reason);
        }
    }

    [Fact]
    public async Task SendAsync_parses_failure_reason()
    {
        var (client, _, provider, key) = CreateClient((_, _) =>
            StubHttpMessageHandler.Error(HttpStatusCode.BadRequest, """{"reason":"BadDeviceToken"}"""));
        using (client)
        using (provider)
        using (key)
        {
            var result = await client.SendAsync(ApnNotification.Alert("tok", "T", "B"));

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("BadDeviceToken", result.Reason);
            Assert.True(result.ShouldRemoveToken);
        }
    }

    [Fact]
    public async Task SendAsync_parses_410_timestamp()
    {
        const long unixMillis = 1_700_000_000_000;
        var (client, _, provider, key) = CreateClient((_, _) =>
            StubHttpMessageHandler.Error((HttpStatusCode)410, $$"""{"reason":"Unregistered","timestamp":{{unixMillis}}}"""));
        using (client)
        using (provider)
        using (key)
        {
            var result = await client.SendAsync(ApnNotification.Alert("tok", "T", "B"));

            Assert.Equal(410, result.StatusCode);
            Assert.Equal("Unregistered", result.Reason);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(unixMillis), result.Timestamp);
            Assert.True(result.ShouldRemoveToken);
        }
    }

    [Fact]
    public async Task SendManyAsync_returns_one_result_per_notification_in_order()
    {
        // Echo the device token back via the apns-id response header so we can assert ordering.
        var (client, _, provider, key) = CreateClient((request, _) =>
        {
            var token = request.RequestUri!.AbsolutePath.Split('/')[^1];
            return StubHttpMessageHandler.Ok(apnsId: token);
        });

        using (client)
        using (provider)
        using (key)
        {
            var notifications = Enumerable.Range(0, 25)
                .Select(i => ApnNotification.Alert($"token-{i}", "T", "B"))
                .ToList();

            var results = await client.SendManyAsync(notifications, maxConcurrency: 8);

            Assert.Equal(25, results.Count);
            for (var i = 0; i < results.Count; i++)
            {
                Assert.True(results[i].IsSuccess);
                Assert.Equal($"token-{i}", results[i].ApnsId);
            }
        }
    }
}
