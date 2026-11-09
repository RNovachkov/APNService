using APNCore;
using APNCore.Models;
using Xunit;

namespace APNService.Tests;

public sealed class EnvironmentTests
{
    [Fact]
    public void Production_uses_the_production_host_on_443()
    {
        var uri = ApnEnvironment.Production.GetBaseUri();
        Assert.Equal("api.push.apple.com", uri.Host);
        Assert.Equal(443, uri.Port);
    }

    [Fact]
    public void Development_uses_the_sandbox_host()
    {
        var uri = ApnEnvironment.Development.GetBaseUri();
        Assert.Equal("api.sandbox.push.apple.com", uri.Host);
    }

    [Fact]
    public void Alternate_port_switches_to_2197()
    {
        Assert.Equal(2197, ApnEnvironment.Production.GetBaseUri(useAlternatePort: true).Port);
    }
}

public sealed class PushTypeTests
{
    [Theory]
    [InlineData(ApnPushType.Alert, "alert")]
    [InlineData(ApnPushType.Background, "background")]
    [InlineData(ApnPushType.Location, "location")]
    [InlineData(ApnPushType.Voip, "voip")]
    [InlineData(ApnPushType.Complication, "complication")]
    [InlineData(ApnPushType.FileProvider, "fileprovider")]
    [InlineData(ApnPushType.Mdm, "mdm")]
    [InlineData(ApnPushType.LiveActivity, "liveactivity")]
    [InlineData(ApnPushType.PushToTalk, "pushtotalk")]
    public void ToHeaderValue_maps_every_push_type(ApnPushType pushType, string expected)
    {
        Assert.Equal(expected, pushType.ToHeaderValue());
    }
}

public sealed class NotificationFactoryTests
{
    [Fact]
    public void Alert_sets_alert_push_type_and_high_priority()
    {
        var n = ApnNotification.Alert("tok", "Title", "Body");

        Assert.Equal(ApnPushType.Alert, n.PushType);
        Assert.Equal(ApnPriority.High, n.Priority);
        Assert.Equal("Title", n.Payload.Aps.Alert!.Title);
        Assert.Equal("Body", n.Payload.Aps.Alert.Body);
    }

    [Fact]
    public void Background_sets_content_available_and_medium_priority()
    {
        var n = ApnNotification.Background("tok", new Dictionary<string, object?> { ["k"] = "v" });

        Assert.Equal(ApnPushType.Background, n.PushType);
        Assert.Equal(ApnPriority.Medium, n.Priority);
        Assert.Equal(1, n.Payload.Aps.ContentAvailable);
        Assert.Null(n.Payload.Aps.Alert);
        Assert.Equal("v", n.Payload.CustomData!["k"]);
    }

    [Fact]
    public void Voip_sets_the_voip_topic()
    {
        var n = ApnNotification.Voip("tok", "com.example.App.voip");

        Assert.Equal(ApnPushType.Voip, n.PushType);
        Assert.Equal("com.example.App.voip", n.Topic);
    }

    [Fact]
    public void LiveActivity_carries_the_aps_and_topic()
    {
        var aps = new Aps { Event = ApnLiveActivityEvent.End };
        var n = ApnNotification.LiveActivity("tok", "com.example.App.push-type.liveactivity", aps);

        Assert.Equal(ApnPushType.LiveActivity, n.PushType);
        Assert.Equal("com.example.App.push-type.liveactivity", n.Topic);
        Assert.Same(aps, n.Payload.Aps);
    }

    [Fact]
    public void Mdm_puts_push_magic_in_the_mdm_key()
    {
        var n = ApnNotification.Mdm("tok", "mdm.topic", "MAGIC");

        Assert.Equal(ApnPushType.Mdm, n.PushType);
        Assert.Equal("MAGIC", n.Payload.CustomData!["mdm"]);
    }

    [Fact]
    public void Constructor_rejects_blank_device_token()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ApnNotification("", ApnPushType.Alert));
    }
}

public sealed class ApnResponseTests
{
    [Fact]
    public void ShouldRemoveToken_is_true_for_410()
    {
        var r = new ApnResponse { IsSuccess = false, StatusCode = 410, Reason = "Unregistered" };
        Assert.True(r.ShouldRemoveToken);
    }

    [Fact]
    public void ShouldRemoveToken_is_true_for_bad_device_token()
    {
        var r = new ApnResponse { IsSuccess = false, StatusCode = 400, Reason = "BadDeviceToken" };
        Assert.True(r.ShouldRemoveToken);
    }

    [Fact]
    public void ShouldRemoveToken_is_false_for_transient_failures()
    {
        var r = new ApnResponse { IsSuccess = false, StatusCode = 429, Reason = "TooManyRequests" };
        Assert.False(r.ShouldRemoveToken);
    }

    [Fact]
    public void EnsureSuccess_throws_on_failure_and_carries_the_response()
    {
        var r = new ApnResponse { IsSuccess = false, StatusCode = 400, Reason = "BadDeviceToken" };
        var ex = Assert.Throws<ApnException>(() => r.EnsureSuccess());
        Assert.Same(r, ex.Response);
        Assert.Contains("BadDeviceToken", ex.Message);
    }

    [Fact]
    public void EnsureSuccess_returns_the_response_on_success()
    {
        var r = new ApnResponse { IsSuccess = true, StatusCode = 200 };
        Assert.Same(r, r.EnsureSuccess());
    }
}
