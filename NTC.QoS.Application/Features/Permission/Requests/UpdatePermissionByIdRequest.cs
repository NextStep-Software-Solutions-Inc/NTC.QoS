using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Requests;

public class UpdatePermissionByIdRequest(Guid id, UpdatePermissionDto permission) : IRequest<ApiResponse<PermissionDto>>
{
    public Guid Id { get; set; } = id;
    public UpdatePermissionDto Permission { get; set; } = permission;
}
