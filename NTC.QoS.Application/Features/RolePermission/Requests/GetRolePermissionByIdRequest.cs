using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Requests;

public class GetRolePermissionByIdRequest(Guid id) : IRequest<ApiResponse<RolePermissionDto>>
{
    public Guid Id { get; set; } = id;
}
