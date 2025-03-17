using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class CreateUserRequest : IRequest<ApiResponse<CreateUserDto>>
{
    public required CreateUserDto User { get; set; }
}
