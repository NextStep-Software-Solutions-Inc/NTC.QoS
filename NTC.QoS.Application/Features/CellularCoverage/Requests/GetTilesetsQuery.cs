using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MapTiler;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class GetTilesetsQuery : IRequest<ApiResponse<List<CellularCoverageTilesetDto>>>
{
    public Guid? RegionId { get; set; }
}
