using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Requests;

public class GetRoleByIdRequest(Guid id) : IRequest<ApiResponse<RoleDto>>
{
    public Guid Id { get; set; } = id;
}