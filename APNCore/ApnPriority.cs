namespace APNCore;

/// <summary>
/// The value of the <c>apns-priority</c> header, controlling how aggressively APNs delivers a
/// notification.
/// </summary>
public enum ApnPriority
{
    /// <summary>
    /// Priority 1 — prioritizes the device's power considerations over all other factors for
    /// delivery, and may delay delivery.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Priority 5 — sends the notification based on power considerations on the user's device.
    /// Required for background (<c>content-available</c>) notifications.
    /// </summary>
    Medium = 5,

    /// <summary>
    /// Priority 10 — sends the notification immediately. Must not be used for background pushes.
    /// </summary>
    High = 10,
}
