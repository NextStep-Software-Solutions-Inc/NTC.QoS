using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class CreateUserRequest(CreateUserDto user) : IRequest<ApiResponse<UserDto>>
{
    public CreateUserDto User { get; set; } = user;
}
