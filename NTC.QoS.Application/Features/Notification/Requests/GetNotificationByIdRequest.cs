using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Notification;

namespace NTC.QoS.Application.Features.Notification.Requests;

public record GetNotificationByIdRequest(Guid Id) : IRequest<ApiResponse<NotificationDto>>;
