using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Requests;

public class CreatePermissionRequest : IRequest<ApiResponse<CreatePermissionDto>>
{
    public required CreatePermissionDto Permission { get; set; }
}
