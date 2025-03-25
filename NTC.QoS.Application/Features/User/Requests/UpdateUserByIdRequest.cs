using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class UpdateUserByIdRequest(Guid id, UpdateUserDto user) : IRequest<ApiResponse<UserDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateUserDto User { get; set; } = user;
}
