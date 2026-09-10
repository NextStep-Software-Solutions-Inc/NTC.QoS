using MediatR;
using NTC.QoS.Application.Features.Notification.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Notification.Handlers.Commands;

public class MarkAllNotificationsAsReadHandler(
    INotificationRepository notificationRepository,
    IUserAuthorizationService userAuthorizationService)
    : IRequestHandler<MarkAllNotificationsAsReadRequest, ApiResponse<int>>
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<int>> Handle(MarkAllNotificationsAsReadRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _userAuthorizationService.GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return new ApiResponse<int>("Unauthorized.");
        }

        var query = _notificationRepository.Query(n => n.RecipientUserId == currentUser.Id && !n.IsRead);
        var unreadNotifications = await _notificationRepository.ToListAsync(query, cancellationToken);

        if (unreadNotifications.Count == 0)
        {
            return new ApiResponse<int>
            {
                Succeeded = true,
                Message = "No unread notifications.",
                Data = 0
            };
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<int>
        {
            Succeeded = true,
            Message = $"{unreadNotifications.Count} notification(s) marked as read.",
            Data = unreadNotifications.Count
        };
    }
}
