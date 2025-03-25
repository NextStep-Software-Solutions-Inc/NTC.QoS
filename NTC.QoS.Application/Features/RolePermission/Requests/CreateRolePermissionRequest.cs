using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Requests;

public class CreateRolePermissionRequest : IRequest<ApiResponse<CreateRolePermissionDto>>
{
    public required CreateRolePermissionDto RolePermission { get; set; }
}
