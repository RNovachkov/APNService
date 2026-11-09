namespace APNCore;

/// <summary>
/// The value of the required <c>apns-push-type</c> header, describing the contents of a
/// notification's payload. Every notification must declare its push type.
/// </summary>
public enum ApnPushType
{
    /// <summary>A notification that displays an alert, plays a sound and/or badges the icon.</summary>
    Alert,

    /// <summary>A silent, content-available notification that wakes the app in the background.</summary>
    Background,

    /// <summary>A notification that requests the device's location (requires the location entitlement).</summary>
    Location,

    /// <summary>A notification that triggers a VoIP call (requires a VoIP services certificate/PushKit).</summary>
    Voip,

    /// <summary>An update for a watchOS complication timeline.</summary>
    Complication,

    /// <summary>A signal to a File Provider extension to reload its enumerator.</summary>
    FileProvider,

    /// <summary>A Mobile Device Management (MDM) notification.</summary>
    Mdm,

    /// <summary>An update to a Live Activity (ActivityKit).</summary>
    LiveActivity,

    /// <summary>A Push to Talk channel notification.</summary>
    PushToTalk,
}

/// <summary>Extensions for <see cref="ApnPushType"/>.</summary>
public static class ApnPushTypeExtensions
{
    /// <summary>Returns the literal value expected by the <c>apns-push-type</c> header.</summary>
    public static string ToHeaderValue(this ApnPushType pushType) => pushType switch
    {
        ApnPushType.Alert => "alert",
        ApnPushType.Background => "background",
        ApnPushType.Location => "location",
        ApnPushType.Voip => "voip",
        ApnPushType.Complication => "complication",
        ApnPushType.FileProvider => "fileprovider",
        ApnPushType.Mdm => "mdm",
        ApnPushType.LiveActivity => "liveactivity",
        ApnPushType.PushToTalk => "pushtotalk",
        _ => throw new ArgumentOutOfRangeException(nameof(pushType), pushType, "Unknown APNs push type."),
    };
}
