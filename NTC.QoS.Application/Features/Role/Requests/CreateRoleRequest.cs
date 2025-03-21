using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Requests;

public class CreateRoleRequest(CreateRoleDto role) : IRequest<ApiResponse<RoleDto>>
{
    public CreateRoleDto Role { get; set; } = role;
}
