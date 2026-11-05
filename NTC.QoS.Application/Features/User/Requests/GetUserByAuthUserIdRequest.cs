using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class GetUserByAuthUserIdRequest(string authUserId, bool allowProvisioning = true) : IRequest<ApiResponse<UserDto>>
{
    public string AuthUserId { get; set; } = authUserId;
    public bool AllowProvisioning { get; set; } = allowProvisioning;
}
