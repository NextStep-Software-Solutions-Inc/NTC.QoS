using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Requests;

public class UpdateRoleByIdRequest(Guid id, UpdateRoleDto role) : IRequest<ApiResponse<RoleDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateRoleDto Role { get; set; } = role;
}
