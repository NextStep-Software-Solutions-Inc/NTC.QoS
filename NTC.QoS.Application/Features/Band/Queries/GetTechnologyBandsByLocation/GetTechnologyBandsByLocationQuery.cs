using MediatR;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Band.Queries.GetTechnologyBandsByLocation;

public record GetTechnologyBandsByLocationQuery(
    string LocationId,
    string LocationType
) : IRequest<DetailedTechnologyBandBreakdownDto>;
