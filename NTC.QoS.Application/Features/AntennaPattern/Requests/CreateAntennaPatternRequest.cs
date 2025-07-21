using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Requests;

public class CreateAntennaPatternRequest : IRequest<ApiResponse<AntennaPatternDto>>
{
    public required CreateAntennaPatternDto AntennaPattern { get; set; }
}