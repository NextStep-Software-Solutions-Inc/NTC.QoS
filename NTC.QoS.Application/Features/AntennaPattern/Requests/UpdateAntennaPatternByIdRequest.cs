using MediatR;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Requests;

public class UpdateAntennaPatternByIdRequest : IRequest<ApiResponse<UpdateAntennaPatternDto>>
{
    public Guid Id { get; set; }

    public required UpdateAntennaPatternDto AntennaPattern { get; set; }
}
