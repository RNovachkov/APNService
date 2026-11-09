namespace APNCore;

/// <summary>
/// The result of attempting to send a notification to APNs.
/// </summary>
public sealed record ApnResponse
{
    /// <summary><see langword="true"/> when APNs accepted the notification (HTTP 200).</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>The HTTP status code returned by APNs.</summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// The notification's unique ID, from the <c>apns-id</c> response header. This matches the
    /// <see cref="Models.ApnNotification.Id"/> you sent, or the one APNs generated.
    /// </summary>
    public string? ApnsId { get; init; }

    /// <summary>
    /// The collapse ID echoed by APNs on the development server via <c>apns-unique-id</c>, useful
    /// when diagnosing delivery with Apple.
    /// </summary>
    public string? ApnsUniqueId { get; init; }

    /// <summary>
    /// On failure, the machine-readable <c>reason</c> string from the response body (for example
    /// <c>BadDeviceToken</c> or <c>Unregistered</c>). <see langword="null"/> on success.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// For a <c>410 Unregistered</c> response, the time at which the device token became invalid.
    /// Stop sending to the token if your records show it has not re-registered since this time.
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>
    /// <see langword="true"/> when the response indicates the device token is permanently invalid
    /// (HTTP 410, or 400 <c>BadDeviceToken</c>) and should be removed from your database.
    /// </summary>
    public bool ShouldRemoveToken =>
        StatusCode == 410 ||
        string.Equals(Reason, "BadDeviceToken", StringComparison.Ordinal) ||
        string.Equals(Reason, "Unregistered", StringComparison.Ordinal);

    /// <summary>Throws an <see cref="ApnException"/> when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public ApnResponse EnsureSuccess()
    {
        if (!IsSuccess)
            throw new ApnException(this);
        return this;
    }
}
