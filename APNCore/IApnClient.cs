using APNCore.Models;

namespace APNCore;

/// <summary>
/// Sends notifications to the Apple Push Notification service (APNs).
/// </summary>
public interface IApnClient
{
    /// <summary>
    /// Sends a single notification to APNs and returns the result. Implementations do not throw for
    /// APNs-level rejections (invalid token, etc.); inspect <see cref="ApnResponse.IsSuccess"/> or
    /// call <see cref="ApnResponse.EnsureSuccess"/>.
    /// </summary>
    /// <param name="notification">The notification to deliver.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    Task<ApnResponse> SendAsync(ApnNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends many notifications concurrently, returning a result for each in the same order as the
    /// input. Failures are reported per-notification rather than throwing.
    /// </summary>
    /// <param name="notifications">The notifications to deliver.</param>
    /// <param name="maxConcurrency">The maximum number of in-flight requests. Defaults to 10.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<ApnResponse>> SendManyAsync(
        IReadOnlyList<ApnNotification> notifications,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default);
}
