# APNService

A lightweight .NET 10 library for sending notifications through the **Apple Push Notification service (APNs)** using token-based (`.p8`) authentication over HTTP/2.

- **Token-based auth** — signs provider JWTs (ES256) from your `.p8` key and caches them within Apple's refresh window.
- **Every push type** — alert, background, VoIP, Live Activity, location, complication, File Provider, MDM, and Push to Talk.
- **Strongly-typed payloads** — a full `aps` model plus arbitrary custom top-level keys.
- **Long-lived and thread-safe** — reuses one HTTP/2 connection and one cached token across sends.
- **First-party dependencies only** — just the .NET base class library and the `Microsoft.Extensions.*` abstractions used by the optional `AddApns()` helper.

## Install

```bash
dotnet add package APNService
```

## Prerequisites

From the [Apple Developer portal](https://developer.apple.com/account/resources/authkeys/list) you need:

| Value | Where it comes from |
| --- | --- |
| **`.p8` key file** | *Keys* → create a key with the *Apple Push Notifications service (APNs)* capability. Download it once (`AuthKey_XXXXXXXXXX.p8`). |
| **Key ID** | The 10-character ID of that key. |
| **Team ID** | Your 10-character Apple Developer Team ID. |
| **Topic** | Usually your app's bundle ID (e.g. `com.example.app`). Some push types need a suffixed topic. |

## Quick start

```csharp
using APNService;
using APNCore;
using APNCore.Models;

// 1. Create a JWT provider from your .p8 key (reuse this; it caches the signed token).
using var jwtProvider = ApnJwtProvider.FromP8File(
    teamId: "ABCDE12345",
    keyId:  "KEY1234567",
    p8FilePath: "/secure/AuthKey_KEY1234567.p8");

// 2. Configure and create a client (long-lived — create once and reuse).
var options = new ApnClientOptions
{
    JwtProvider  = jwtProvider,
    Environment  = ApnEnvironment.Development, // or ApnEnvironment.Production
    DefaultTopic = "com.example.app",          // your bundle ID
};
using var client = new ApnClient(options);

// 3. Send an alert.
var notification = ApnNotification.Alert(
    deviceToken: "<64-hex-char device token>",
    title: "Hello",
    body:  "World");

ApnResponse response = await client.SendAsync(notification);

if (response.IsSuccess)
    Console.WriteLine($"Delivered. apns-id = {response.ApnsId}");
else
    Console.WriteLine($"Rejected ({response.StatusCode}): {response.Reason}");
```

`SendAsync` does **not** throw when APNs rejects a notification — inspect `response.IsSuccess` /
`response.Reason`, or call `response.EnsureSuccess()` to turn a rejection into an `ApnException`.

## Handling responses and pruning dead tokens

When a device uninstalls your app, APNs reports the token as invalid. Remove such tokens from your
database so you stop sending to them:

```csharp
var response = await client.SendAsync(notification);

if (response.ShouldRemoveToken)
{
    // HTTP 410 Unregistered, or 400 BadDeviceToken — the token is permanently invalid.
    await tokenStore.RemoveAsync(notification.DeviceToken);
}
```

`ApnResponse` exposes `IsSuccess`, `StatusCode`, `ApnsId`, `Reason`, `Timestamp` (for `410`), and the
convenience `ShouldRemoveToken`.

## Sending to many devices

```csharp
var notifications = tokens
    .Select(t => ApnNotification.Alert(t, "Sale!", "50% off today only"))
    .ToList();

IReadOnlyList<ApnResponse> results =
    await client.SendManyAsync(notifications, maxConcurrency: 20);
```

Results are returned in the same order as the input; each is reported individually (no throwing).

## Other push types

Factory helpers cover the common cases. Each returns an `ApnNotification` you can further customize
via its `Payload`.

```csharp
// Silent background refresh (content-available, medium priority).
var bg = ApnNotification.Background(token, new Dictionary<string, object?> { ["sync"] = true });

// VoIP (PushKit) — topic must be your VoIP topic.
var voip = ApnNotification.Voip(token, "com.example.app.voip");

// Live Activity (ActivityKit).
var live = ApnNotification.LiveActivity(token, "com.example.app.push-type.liveactivity", new Aps
{
    Event        = ApnLiveActivityEvent.Update,
    ContentState = new { progress = 0.7, status = "On the way" },
    Timestamp    = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
});
```

Also available: `Location`, `Complication`, `FileProvider`, `Mdm`, and `PushToTalk`.

## Building a payload by hand

```csharp
var notification = new ApnNotification(token, ApnPushType.Alert)
{
    Priority = ApnPriority.High,
    Payload  = new ApnPayload
    {
        Aps = new Aps
        {
            Alert = new ApnAlert { Title = "Title", Subtitle = "Subtitle", Body = "Body" },
            Badge = 3,
            Sound = "default",
            ThreadId = "chat-42",
            InterruptionLevel = ApnInterruptionLevel.TimeSensitive,
        },
    },
};

// Custom top-level keys (anything other than "aps"):
notification.Payload.WithCustom("conversationId", 42);
```

## Dependency injection

The quickest way to wire everything up is `AddApns()`, which registers `IApnClient` (and its
`ApnJwtProvider`) as singletons:

```csharp
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddApns(apns =>
{
    apns.TeamId       = "ABCDE12345";
    apns.KeyId        = "KEY1234567";
    apns.P8FilePath   = "/secure/AuthKey_KEY1234567.p8";
    apns.Environment  = ApnEnvironment.Production;
    apns.DefaultTopic = "com.example.app";
});
```

Then inject `IApnClient` wherever you need it:

```csharp
public sealed class NotificationService(IApnClient apns)
{
    public Task<ApnResponse> PingAsync(string token) =>
        apns.SendAsync(ApnNotification.Alert(token, "Hi", "There"));
}
```

### Binding from configuration

Bind the settings from an `Apns` section of `appsettings.json`:

```jsonc
// appsettings.json
{
  "Apns": {
    "TeamId": "ABCDE12345",
    "KeyId": "KEY1234567",
    "P8FilePath": "/secure/AuthKey_KEY1234567.p8",
    "Environment": "Production",
    "DefaultTopic": "com.example.app",
    "UseAlternatePort": false,
    "TimeoutSeconds": 30
  }
}
```

```csharp
builder.Services.AddApns(builder.Configuration.GetSection("Apns"));
```

### Manual registration

If you prefer to construct the client yourself:

```csharp
builder.Services.AddSingleton(_ =>
    ApnJwtProvider.FromP8File("ABCDE12345", "KEY1234567", "/secure/AuthKey_KEY1234567.p8"));

builder.Services.AddSingleton<IApnClient>(sp => new ApnClient(new ApnClientOptions
{
    JwtProvider  = sp.GetRequiredService<ApnJwtProvider>(),
    Environment  = ApnEnvironment.Production,
    DefaultTopic = "com.example.app",
}));
```

### Using `IHttpClientFactory`

Pass your own `HttpClient` (configure it for HTTP/2); the client will not dispose it:

```csharp
builder.Services.AddHttpClient("apns", http =>
{
    http.DefaultRequestVersion = HttpVersion.Version20;
    http.DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionExact;
});

builder.Services.AddSingleton<IApnClient>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("apns");
    return new ApnClient(options, http, ownsHttpClient: false);
});
```

## Environments

| `ApnEnvironment` | Host | Use for |
| --- | --- | --- |
| `Development` | `api.sandbox.push.apple.com` | Apps built with a development provisioning profile. |
| `Production` | `api.push.apple.com` | TestFlight and App Store builds. |

Set `ApnClientOptions.UseAlternatePort = true` to use port **2197** instead of 443 (useful behind
firewalls that block the standard HTTPS port).

## Notes

- **Reuse the client.** Create one `ApnClient` per environment and keep it for the lifetime of your
  app so the HTTP/2 connection and cached provider token are shared.
- **Thread safety.** `ApnJwtProvider`, `ApnClient`, and `SendManyAsync` are safe to use concurrently.
- **Token refresh.** The provider signs a new JWT lazily and reuses it for 40 minutes, inside Apple's
  20–60 minute refresh window.

## License

Licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
