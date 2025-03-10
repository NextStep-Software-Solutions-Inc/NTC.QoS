using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Requests;

public class GetAntennaPatternByIdRequest(Guid id) : IRequest<ApiResponse<AntennaPatternDto>>
{
    public Guid Id { get; set; } = id;
}