using MediatR;
using NTC.QoS.Application.Features.Notification.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Notification;

namespace NTC.QoS.Application.Features.Notification.Handlers.Queries;

public class GetAllNotificationsHandler(
    INotificationRepository notificationRepository,
    IUserAuthorizationService userAuthorizationService)
    : IRequestHandler<GetAllNotificationsRequest, PaginatedResponse<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<PaginatedResponse<NotificationDto>> Handle(GetAllNotificationsRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _userAuthorizationService.GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return new PaginatedResponse<NotificationDto>([], request.PageNumber, request.PageSize, 0);
        }

        var query = _notificationRepository.Query(n => n.RecipientUserId == currentUser.Id);

        if (request.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == request.IsRead.Value);
        }

        query = query.OrderByDescending(n => n.CreatedAt);

        var projected = query.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            RelatedEntityId = n.RelatedEntityId,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreatedAt = n.CreatedAt
        });

        return await _notificationRepository.PaginateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
