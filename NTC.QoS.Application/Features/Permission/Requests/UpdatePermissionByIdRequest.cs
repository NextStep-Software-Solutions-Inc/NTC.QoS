using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Requests;

public class UpdatePermissionByIdRequest : IRequest<ApiResponse<UpdatePermissionDto>>
{
    public Guid Id { get; set; }
    public required UpdatePermissionDto Permission { get; set; }
}
