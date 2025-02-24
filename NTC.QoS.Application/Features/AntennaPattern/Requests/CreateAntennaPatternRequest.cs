using MediatR;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Requests;

public class CreateAntennaPatternRequest : IRequest<ApiResponse<CreateAntennaPatternDto>>
{
    public required CreateAntennaPatternDto AntennaPattern { get; set; }
}