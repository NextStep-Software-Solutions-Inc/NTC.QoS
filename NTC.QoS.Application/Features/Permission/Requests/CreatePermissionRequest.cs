using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Requests;

public class CreatePermissionRequest(CreatePermissionDto permission) : IRequest<ApiResponse<PermissionDto>>
{
    public CreatePermissionDto Permission { get; set; } = permission;
}
