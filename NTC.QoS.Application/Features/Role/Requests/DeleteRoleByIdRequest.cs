using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Role.Requests;

public class DeleteRoleByIdRequest(Guid id) : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; } = id;
}


