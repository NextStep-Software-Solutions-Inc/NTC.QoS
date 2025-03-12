using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Requests;

public class CreateRoleRequest : IRequest<ApiResponse<CreateRoleDto>>
{
    public required CreateRoleDto Role { get; set; }
}