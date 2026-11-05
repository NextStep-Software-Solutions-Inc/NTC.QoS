using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class InviteUserRequest(InviteUserDto user, bool allowReinvite = false) : IRequest<ApiResponse<UserDto>>
{
    public InviteUserDto User { get; set; } = user;
    public bool AllowReinvite { get; set; } = allowReinvite;
}
