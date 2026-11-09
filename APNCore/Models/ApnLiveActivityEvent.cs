using System.Text.Json.Serialization;

namespace APNCore.Models;

/// <summary>
/// The <c>event</c> value for a Live Activity (<see cref="ApnPushType.LiveActivity"/>) notification.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApnLiveActivityEvent>))]
public enum ApnLiveActivityEvent
{
    /// <summary>Starts a new Live Activity remotely. Requires <c>attributes-type</c> and <c>attributes</c>.</summary>
    [JsonStringEnumMemberName("start")]
    Start,

    /// <summary>Updates the content state of a running Live Activity.</summary>
    [JsonStringEnumMemberName("update")]
    Update,

    /// <summary>Ends a running Live Activity.</summary>
    [JsonStringEnumMemberName("end")]
    End,
}
