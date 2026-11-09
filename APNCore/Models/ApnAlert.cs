using System.Text.Json.Serialization;

namespace APNCore.Models;

/// <summary>
/// The rich, dictionary form of the <c>aps.alert</c> field. Use this when you need titles,
/// subtitles or localized strings. For a simple message, set <see cref="Aps.AlertBody"/> instead.
/// </summary>
public sealed class ApnAlert
{
    /// <summary>A short string describing the purpose of the notification.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Additional information that explains the purpose of the notification.</summary>
    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    /// <summary>The content of the alert message.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>The name of the launch image file to display when your app launches from the notification.</summary>
    [JsonPropertyName("launch-image")]
    public string? LaunchImage { get; set; }

    /// <summary>The key for a localized <see cref="Title"/> string in your app's <c>Localizable.strings</c>.</summary>
    [JsonPropertyName("title-loc-key")]
    public string? TitleLocKey { get; set; }

    /// <summary>Variable string values to replace the format specifiers in the <see cref="TitleLocKey"/> string.</summary>
    [JsonPropertyName("title-loc-args")]
    public IReadOnlyList<string>? TitleLocArgs { get; set; }

    /// <summary>The key for a localized <see cref="Subtitle"/> string.</summary>
    [JsonPropertyName("subtitle-loc-key")]
    public string? SubtitleLocKey { get; set; }

    /// <summary>Variable string values to replace the format specifiers in the <see cref="SubtitleLocKey"/> string.</summary>
    [JsonPropertyName("subtitle-loc-args")]
    public IReadOnlyList<string>? SubtitleLocArgs { get; set; }

    /// <summary>The key for a localized <see cref="Body"/> string.</summary>
    [JsonPropertyName("loc-key")]
    public string? LocKey { get; set; }

    /// <summary>Variable string values to replace the format specifiers in the <see cref="LocKey"/> string.</summary>
    [JsonPropertyName("loc-args")]
    public IReadOnlyList<string>? LocArgs { get; set; }

    /// <summary>
    /// The string the system displays as the action button title (if actions are supported), using a
    /// key in your app's <c>Localizable.strings</c>.
    /// </summary>
    [JsonPropertyName("action-loc-key")]
    public string? ActionLocKey { get; set; }
}
