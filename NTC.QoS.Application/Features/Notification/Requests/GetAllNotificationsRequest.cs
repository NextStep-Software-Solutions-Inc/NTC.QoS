using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Notification;

namespace NTC.QoS.Application.Features.Notification.Requests;

public record GetAllNotificationsRequest : IRequest<PaginatedResponse<NotificationDto>>
{
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Filter by read status. Null returns both read and unread.
    /// </summary>
    public bool? IsRead { get; set; }
}
