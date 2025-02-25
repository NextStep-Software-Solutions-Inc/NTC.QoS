using MediatR;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Requests;

public class GetAntennaPatternByIdRequest : IRequest<ApiResponse<AntennaPatternDto>>
{
    public GetAntennaPatternByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}