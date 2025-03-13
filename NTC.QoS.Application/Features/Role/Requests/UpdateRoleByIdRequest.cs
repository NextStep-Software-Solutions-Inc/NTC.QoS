using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Requests;

public class UpdateRoleByIdRequest : IRequest<ApiResponse<UpdateRoleDto>>
{
    public Guid Id { get; set; }
    public required UpdateRoleDto Role { get; set; }
}
