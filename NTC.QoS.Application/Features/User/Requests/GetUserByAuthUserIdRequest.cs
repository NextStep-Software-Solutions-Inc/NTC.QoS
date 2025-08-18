using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class GetUserByAuthUserIdRequest(string authUserId) : IRequest<ApiResponse<UserDto>>
{
    public string AuthUserId { get; set; } = authUserId;
}
