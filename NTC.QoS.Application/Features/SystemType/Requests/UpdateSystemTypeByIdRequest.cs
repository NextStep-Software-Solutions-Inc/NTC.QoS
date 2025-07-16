using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Requests;

public class UpdateSystemTypeByIdRequest(Guid id, UpdateSystemTypeDto systemType) : IRequest<ApiResponse<SystemTypeDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateSystemTypeDto SystemType { get; set; } = systemType;
}
