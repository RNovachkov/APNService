using System.Text.Json.Serialization;

namespace APNCore.Models;

/// <summary>
/// The <c>interruption-level</c> of an alert, indicating the importance and delivery timing of a
/// notification (iOS 15+).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApnInterruptionLevel>))]
public enum ApnInterruptionLevel
{
    /// <summary>The notification is added to the list silently, without lighting up the screen or playing a sound.</summary>
    [JsonStringEnumMemberName("passive")]
    Passive,

    /// <summary>The system presents the notification immediately, may play a sound, and lights up the screen (default).</summary>
    [JsonStringEnumMemberName("active")]
    Active,

    /// <summary>The system presents the notification immediately and may break through Focus/Do Not Disturb.</summary>
    [JsonStringEnumMemberName("time-sensitive")]
    TimeSensitive,

    /// <summary>The system presents the notification immediately, bypasses the mute switch and Focus, and may play a critical sound.</summary>
    [JsonStringEnumMemberName("critical")]
    Critical,
}
