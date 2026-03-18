using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Creates and persists in-app notifications for users.
/// </summary>
public class NotificationService(
    INotificationRepository notificationRepository,
    ApplicationDbContext dbContext,
    ILogger<NotificationService> logger)
    : INotificationService
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<NotificationService> _logger = logger;

    public async Task NotifyRegionAccessRequestAsync(
        Guid requestingUserId,
        string requestingUserEmail,
        Guid regionId,
        string regionName,
        Guid userRegionId,
        CancellationToken cancellationToken = default)
    {
        var superAdminIds = await GetSuperAdminUserIdsAsync(cancellationToken);

        if (superAdminIds.Count == 0)
        {
            _logger.LogWarning("No super-admin users found to notify for region access request. UserRegionId: {UserRegionId}", userRegionId);
            return;
        }

        var title = "New Region Access Request";
        var message = $"User '{requestingUserEmail}' has requested access to region '{regionName}'.";

        var notifications = superAdminIds.Select(recipientId => new Notification
        {
            RecipientUserId = recipientId,
            Type = NotificationType.RegionAccessRequest.ToString(),
            Title = title,
            Message = message,
            RelatedEntityId = userRegionId,
            IsRead = false
        }).ToList();

        _notificationRepository.AddRange(notifications);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sent {Count} region access request notification(s) to super-admins for UserRegionId: {UserRegionId}",
            notifications.Count, userRegionId);
    }

    public async Task NotifyRegionAccessGrantedAsync(
        Guid recipientUserId,
        string regionName,
        Guid userRegionId,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = NotificationType.RegionAccessGranted.ToString(),
            Title = "Region Access Granted",
            Message = $"Your access request for region '{regionName}' has been approved.",
            RelatedEntityId = userRegionId,
            IsRead = false
        };

        _notificationRepository.Add(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sent region access granted notification to UserId: {UserId} for UserRegionId: {UserRegionId}",
            recipientUserId, userRegionId);
    }

    public async Task NotifyRegionAccessDeclinedAsync(
        Guid recipientUserId,
        string regionName,
        Guid userRegionId,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = NotificationType.RegionAccessDeclined.ToString(),
            Title = "Region Access Declined",
            Message = $"Your access request for region '{regionName}' has been declined.",
            RelatedEntityId = userRegionId,
            IsRead = false
        };

        _notificationRepository.Add(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sent region access declined notification to UserId: {UserId} for UserRegionId: {UserRegionId}",
            recipientUserId, userRegionId);
    }

    /// <summary>
    /// Retrieves the IDs of all active super-admin users.
    /// </summary>
    private async Task<List<Guid>> GetSuperAdminUserIdsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Set<User>()
            .Where(u => !u.IsDeleted && u.Role.Key == Roles.SuperAdmin)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }
}
