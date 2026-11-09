using System.Text.Json;
using APNCore;
using APNCore.Models;
using Xunit;

namespace APNService.Tests;

public sealed class ApnPayloadSerializationTests
{
    private static readonly JsonSerializerOptions Options = ApnClientOptions.CreateDefaultJsonOptions();

    private static JsonElement Serialize(ApnPayload payload)
    {
        var json = payload.ToJson(Options);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void Reserved_keys_use_apple_kebab_case_names()
    {
        var payload = new ApnPayload
        {
            Aps = new Aps
            {
                ContentAvailable = 1,
                MutableContent = 1,
                ThreadId = "t1",
                TargetContentId = "win-1",
                InterruptionLevel = ApnInterruptionLevel.TimeSensitive,
                RelevanceScore = 0.5,
            },
        };

        var aps = Serialize(payload).GetProperty("aps");
        Assert.Equal(1, aps.GetProperty("content-available").GetInt32());
        Assert.Equal(1, aps.GetProperty("mutable-content").GetInt32());
        Assert.Equal("t1", aps.GetProperty("thread-id").GetString());
        Assert.Equal("win-1", aps.GetProperty("target-content-id").GetString());
        Assert.Equal("time-sensitive", aps.GetProperty("interruption-level").GetString());
        Assert.Equal(0.5, aps.GetProperty("relevance-score").GetDouble());
    }

    [Fact]
    public void Null_properties_are_omitted()
    {
        var aps = Serialize(new ApnPayload { Aps = new Aps { Badge = 0 } }).GetProperty("aps");

        Assert.Equal(0, aps.GetProperty("badge").GetInt32());
        Assert.False(aps.TryGetProperty("sound", out _));
        Assert.False(aps.TryGetProperty("alert", out _));
        Assert.False(aps.TryGetProperty("content-available", out _));
    }

    [Fact]
    public void Simple_alert_body_serializes_as_a_string()
    {
        var aps = Serialize(new ApnPayload { Aps = new Aps { AlertBody = "hi" } }).GetProperty("aps");

        Assert.Equal(JsonValueKind.String, aps.GetProperty("alert").ValueKind);
        Assert.Equal("hi", aps.GetProperty("alert").GetString());
    }

    [Fact]
    public void Rich_alert_serializes_as_an_object_and_wins_over_body()
    {
        var aps = Serialize(new ApnPayload
        {
            Aps = new Aps
            {
                AlertBody = "ignored",
                Alert = new ApnAlert { Title = "T", Body = "B", LaunchImage = "img", LocArgs = ["a", "b"] },
            },
        }).GetProperty("aps");

        var alert = aps.GetProperty("alert");
        Assert.Equal(JsonValueKind.Object, alert.ValueKind);
        Assert.Equal("T", alert.GetProperty("title").GetString());
        Assert.Equal("B", alert.GetProperty("body").GetString());
        Assert.Equal("img", alert.GetProperty("launch-image").GetString());
        Assert.Equal(2, alert.GetProperty("loc-args").GetArrayLength());
    }

    [Fact]
    public void Simple_sound_serializes_as_a_string()
    {
        var aps = Serialize(new ApnPayload { Aps = new Aps { Sound = "default" } }).GetProperty("aps");

        Assert.Equal(JsonValueKind.String, aps.GetProperty("sound").ValueKind);
        Assert.Equal("default", aps.GetProperty("sound").GetString());
    }

    [Fact]
    public void Critical_sound_serializes_as_an_object_and_wins_over_string()
    {
        var aps = Serialize(new ApnPayload
        {
            Aps = new Aps
            {
                Sound = "ignored",
                CriticalSound = new ApnSound { Name = "siren.caf", Volume = 1.0 },
            },
        }).GetProperty("aps");

        var sound = aps.GetProperty("sound");
        Assert.Equal(JsonValueKind.Object, sound.ValueKind);
        Assert.Equal(1, sound.GetProperty("critical").GetInt32());
        Assert.Equal("siren.caf", sound.GetProperty("name").GetString());
        Assert.Equal(1.0, sound.GetProperty("volume").GetDouble());
    }

    [Fact]
    public void Live_activity_fields_serialize_correctly()
    {
        var aps = Serialize(new ApnPayload
        {
            Aps = new Aps
            {
                Event = ApnLiveActivityEvent.Update,
                Timestamp = 1700,
                StaleDate = 1800,
                ContentState = new { minutesAway = 4 },
            },
        }).GetProperty("aps");

        Assert.Equal("update", aps.GetProperty("event").GetString());
        Assert.Equal(1700, aps.GetProperty("timestamp").GetInt64());
        Assert.Equal(1800, aps.GetProperty("stale-date").GetInt64());
        Assert.Equal(4, aps.GetProperty("content-state").GetProperty("minutesAway").GetInt32());
    }

    [Fact]
    public void Custom_data_is_merged_at_the_root()
    {
        var payload = new ApnPayload { Aps = new Aps { AlertBody = "hi" } }
            .WithCustom("conversationId", 42)
            .WithCustom("deepLink", "app://x");

        var root = Serialize(payload);
        Assert.True(root.TryGetProperty("aps", out _));
        Assert.Equal(42, root.GetProperty("conversationId").GetInt32());
        Assert.Equal("app://x", root.GetProperty("deepLink").GetString());
    }

    [Fact]
    public void WithCustom_rejects_the_reserved_aps_key()
    {
        Assert.Throws<ArgumentException>(() => new ApnPayload().WithCustom("aps", 1));
    }

    [Fact]
    public void ToJsonNode_rejects_a_reserved_aps_custom_key_added_directly()
    {
        var payload = new ApnPayload
        {
            CustomData = new Dictionary<string, object?> { ["aps"] = 1 },
        };

        Assert.Throws<InvalidOperationException>(() => payload.ToJson(Options));
    }
}
