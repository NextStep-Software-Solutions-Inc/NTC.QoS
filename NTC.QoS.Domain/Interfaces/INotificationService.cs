namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service responsible for creating and dispatching in-app notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a region access request notification to all super-admin users.
    /// </summary>
    /// <param name="requestingUserId">The internal DB ID of the user requesting access.</param>
    /// <param name="requestingUserEmail">The email of the requesting user (used in the message).</param>
    /// <param name="regionId">The region being requested.</param>
    /// <param name="regionName">The region name (used in the message).</param>
    /// <param name="userRegionId">The ID of the newly created UserRegion record (used as related entity).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyRegionAccessRequestAsync(
        Guid requestingUserId,
        string requestingUserEmail,
        Guid regionId,
        string regionName,
        Guid userRegionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a specific user informing them that their region access was granted.
    /// </summary>
    Task NotifyRegionAccessGrantedAsync(
        Guid recipientUserId,
        string regionName,
        Guid userRegionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a specific user informing them that their region access was declined.
    /// </summary>
    Task NotifyRegionAccessDeclinedAsync(
        Guid recipientUserId,
        string regionName,
        Guid userRegionId,
        CancellationToken cancellationToken = default);
}
