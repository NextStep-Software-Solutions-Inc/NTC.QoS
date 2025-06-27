using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Requests;

public class CreateSystemTypeRequest : IRequest<ApiResponse<SystemTypeDto>>
{
    public required CreateSystemTypeDto SystemType { get; set; }
}
