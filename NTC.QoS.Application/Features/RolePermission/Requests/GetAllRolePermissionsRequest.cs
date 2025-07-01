using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Requests;

public class GetAllRolePermissionsRequest(int pageNumber, int pageSize, Guid? roleId, Guid? permissionId) : IRequest<PaginatedResponse<RolePermissionDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? RoleId { get; set; } = roleId;
    public Guid? PermissionId { get; set; } = permissionId;
}
