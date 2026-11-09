namespace APNCore;

/// <summary>
/// Thrown when APNs rejects a notification and the caller requested that failures throw (for example
/// via <see cref="ApnResponse.EnsureSuccess"/>).
/// </summary>
public sealed class ApnException : Exception
{
    /// <summary>Creates the exception from a failed <see cref="ApnResponse"/>.</summary>
    public ApnException(ApnResponse response)
        : base($"APNs rejected the notification with status {response.StatusCode}" +
               (response.Reason is { } reason ? $" ({reason})." : "."))
    {
        Response = response;
    }

    /// <summary>The failed response returned by APNs.</summary>
    public ApnResponse Response { get; }
}
