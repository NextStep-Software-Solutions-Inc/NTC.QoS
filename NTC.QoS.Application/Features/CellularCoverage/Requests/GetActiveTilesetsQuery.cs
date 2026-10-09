using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MapTiler;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class GetActiveTilesetsQuery : IRequest<ApiResponse<List<CellularCoverageTilesetDto>>>
{
    public Guid? RegionId { get; set; }
    public Guid? ServiceProviderId { get; set; }
}
