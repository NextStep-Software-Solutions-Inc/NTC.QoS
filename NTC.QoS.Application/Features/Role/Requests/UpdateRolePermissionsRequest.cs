using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Requests;

public class UpdateRolePermissionsRequest(Guid roleId, UpdateRolePermissionsDto payload) : IRequest<ApiResponse<RoleDto>>
{
    public Guid RoleId { get; set; } = roleId;
    public UpdateRolePermissionsDto Payload { get; set; } = payload;
}
