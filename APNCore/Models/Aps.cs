using System.Text.Json.Serialization;

namespace APNCore.Models;

/// <summary>
/// The Apple-reserved <c>aps</c> dictionary of a notification payload. Only the properties you set
/// are serialized.
/// </summary>
public sealed class Aps
{
    // -- Alert --------------------------------------------------------------------------------

    /// <summary>
    /// A simple alert message. Mutually exclusive with <see cref="Alert"/>; if <see cref="Alert"/>
    /// is set it takes precedence.
    /// </summary>
    [JsonIgnore]
    public string? AlertBody { get; set; }

    /// <summary>The rich, dictionary form of the alert (titles, subtitles, localization).</summary>
    [JsonIgnore]
    public ApnAlert? Alert { get; set; }

    /// <summary>The serialized value of <c>aps.alert</c> — either the <see cref="Alert"/> object or the <see cref="AlertBody"/> string.</summary>
    [JsonInclude]
    [JsonPropertyName("alert")]
    internal object? AlertValue => Alert is not null ? Alert : AlertBody;

    // -- Badge & sound ------------------------------------------------------------------------

    /// <summary>
    /// The number to display in a badge on your app's icon. Set to <c>0</c> to remove the badge.
    /// </summary>
    [JsonPropertyName("badge")]
    public int? Badge { get; set; }

    /// <summary>
    /// The name of a sound file to play, or <c>"default"</c> for the system sound. Mutually
    /// exclusive with <see cref="CriticalSound"/>, which takes precedence.
    /// </summary>
    [JsonIgnore]
    public string? Sound { get; set; }

    /// <summary>The dictionary form of the sound, used for critical alerts.</summary>
    [JsonIgnore]
    public ApnSound? CriticalSound { get; set; }

    /// <summary>The serialized value of <c>aps.sound</c>.</summary>
    [JsonInclude]
    [JsonPropertyName("sound")]
    internal object? SoundValue => CriticalSound is not null ? CriticalSound : Sound;

    // -- Behavior -----------------------------------------------------------------------------

    /// <summary>
    /// Set to <c>1</c> for a background (silent) notification that wakes your app to download new
    /// content. Pair with <see cref="ApnPushType.Background"/> and <see cref="ApnPriority.Medium"/>.
    /// </summary>
    [JsonPropertyName("content-available")]
    public int? ContentAvailable { get; set; }

    /// <summary>
    /// Set to <c>1</c> to allow a Notification Service Extension to modify the notification's
    /// content before it is displayed.
    /// </summary>
    [JsonPropertyName("mutable-content")]
    public int? MutableContent { get; set; }

    /// <summary>The identifier of the <c>UNNotificationCategory</c> for actionable notifications.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>A value used to group related notifications together in Notification Center.</summary>
    [JsonPropertyName("thread-id")]
    public string? ThreadId { get; set; }

    /// <summary>The identifier of the window brought forward when the user taps the notification.</summary>
    [JsonPropertyName("target-content-id")]
    public string? TargetContentId { get; set; }

    /// <summary>The importance and delivery timing of the notification (iOS 15+).</summary>
    [JsonPropertyName("interruption-level")]
    public ApnInterruptionLevel? InterruptionLevel { get; set; }

    /// <summary>
    /// A score, from <c>0.0</c> to <c>1.0</c>, the system uses to sort notifications from your app.
    /// The highest-scoring notification is featured in the summary (iOS 15+).
    /// </summary>
    [JsonPropertyName("relevance-score")]
    public double? RelevanceScore { get; set; }

    /// <summary>
    /// The criteria the system evaluates to decide whether to display the notification while a
    /// Focus filter is active (iOS 16+).
    /// </summary>
    [JsonPropertyName("filter-criteria")]
    public string? FilterCriteria { get; set; }

    // -- Live Activity (ActivityKit) ----------------------------------------------------------

    /// <summary>The event for a Live Activity update: <c>start</c>, <c>update</c> or <c>end</c>.</summary>
    [JsonPropertyName("event")]
    public ApnLiveActivityEvent? Event { get; set; }

    /// <summary>
    /// The updated content state for the Live Activity, matching your
    /// <c>ActivityAttributes.ContentState</c> type. Serialized as-is.
    /// </summary>
    [JsonPropertyName("content-state")]
    public object? ContentState { get; set; }

    /// <summary>
    /// The UNIX timestamp (seconds) used to match this update to the correct Live Activity. Set it
    /// to the current time so later updates are not overwritten by earlier ones that arrive late.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }

    /// <summary>The UNIX timestamp (seconds) after which the system considers the Live Activity out of date.</summary>
    [JsonPropertyName("stale-date")]
    public long? StaleDate { get; set; }

    /// <summary>The UNIX timestamp (seconds) at which the system should dismiss an ended Live Activity.</summary>
    [JsonPropertyName("dismissal-date")]
    public long? DismissalDate { get; set; }

    /// <summary>The name of the <c>ActivityAttributes</c> type. Required when <see cref="Event"/> is <see cref="ApnLiveActivityEvent.Start"/>.</summary>
    [JsonPropertyName("attributes-type")]
    public string? AttributesType { get; set; }

    /// <summary>The attributes payload used to start a Live Activity. Required when <see cref="Event"/> is <see cref="ApnLiveActivityEvent.Start"/>.</summary>
    [JsonPropertyName("attributes")]
    public object? Attributes { get; set; }
}
