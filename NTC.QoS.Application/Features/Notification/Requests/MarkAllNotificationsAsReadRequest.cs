using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Notification.Requests;

public record MarkAllNotificationsAsReadRequest : IRequest<ApiResponse<int>>;
