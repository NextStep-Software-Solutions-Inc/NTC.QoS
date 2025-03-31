using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class GetUserByIdRequest(Guid id) : IRequest<ApiResponse<UserDto>>
{
    public Guid Id { get; set; } = id;
}
