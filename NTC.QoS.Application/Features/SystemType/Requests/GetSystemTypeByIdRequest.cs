using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Requests;

public class GetSystemTypeByIdRequest(Guid id) : IRequest<ApiResponse<SystemTypeDto>>
{
    public Guid Id { get; set; } = id;
}
