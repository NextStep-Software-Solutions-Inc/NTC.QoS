using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Requests;

public class GetPermissionByIdRequest(Guid id) : IRequest<ApiResponse<GetPermissionDto>>
{
    public Guid Id { get; set; } = id;
}