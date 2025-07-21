using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class CreateSessionRequest : IRequest<ApiResponse<bool>>
{
    public string AuthUserId { get; set; } = string.Empty;
}
