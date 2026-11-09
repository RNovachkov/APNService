using System.Text.Json.Serialization;

namespace APNCore.Models;

/// <summary>
/// The dictionary form of the <c>aps.sound</c> field, used to play a critical alert sound that
/// ignores the ringer switch and Focus. For a normal sound, set <see cref="Aps.Sound"/> to a file
/// name string instead.
/// </summary>
public sealed class ApnSound
{
    /// <summary>
    /// Set to <c>1</c> to mark the sound as critical. Critical alerts require a special entitlement
    /// from Apple.
    /// </summary>
    [JsonPropertyName("critical")]
    public int Critical { get; set; } = 1;

    /// <summary>
    /// The name of a sound file in your app's main bundle or in the <c>Library/Sounds</c> folder of
    /// your app's container. Use <c>"default"</c> for the system sound.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "default";

    /// <summary>The volume for the critical alert's sound, from <c>0.0</c> (silent) to <c>1.0</c> (full).</summary>
    [JsonPropertyName("volume")]
    public double? Volume { get; set; }
}
