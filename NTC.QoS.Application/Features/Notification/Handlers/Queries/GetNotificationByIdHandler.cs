using MediatR;
using NTC.QoS.Application.Features.Notification.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Notification;

namespace NTC.QoS.Application.Features.Notification.Handlers.Queries;

public class GetNotificationByIdHandler(
    INotificationRepository notificationRepository,
    IUserAuthorizationService userAuthorizationService)
    : IRequestHandler<GetNotificationByIdRequest, ApiResponse<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<NotificationDto>> Handle(GetNotificationByIdRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _userAuthorizationService.GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return new ApiResponse<NotificationDto>("Unauthorized.");
        }

        var query = _notificationRepository.Query(n => n.Id == request.Id && n.RecipientUserId == currentUser.Id);
        var notification = await _notificationRepository.FirstOrDefaultAsync(query, cancellationToken);

        if (notification is null)
        {
            return new ApiResponse<NotificationDto>("Notification not found.");
        }

        return new ApiResponse<NotificationDto>
        {
            Succeeded = true,
            Data = new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                RelatedEntityId = notification.RelatedEntityId,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                CreatedAt = notification.CreatedAt
            }
        };
    }
}
