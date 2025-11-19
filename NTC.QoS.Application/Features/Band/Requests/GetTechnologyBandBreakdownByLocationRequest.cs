using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Band.Requests;

public record GetTechnologyBandBreakdownByLocationRequest : IRequest<ApiResponse<TechnologyBandBreakdownDto>>
{
    public required Guid LocationId { get; init; }
    public LocationTypeEnum LocationType { get; init; } = LocationTypeEnum.Municipality;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}
