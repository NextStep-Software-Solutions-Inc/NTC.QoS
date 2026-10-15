using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Queries;

/// <summary>
/// Returns barangay boundary polygons with wired FTTH fiber coverage data that
/// intersect the requested map viewport, for use as a Fiber Networks map layer.
///
/// IMPORTANT – Map Rendering Note for frontend consumers:
/// The returned GeoJSON polygon features MUST be added to the map BEFORE the
/// map style's symbol/label layers so that place-name labels remain legible.
/// Example (MapLibre/Mapbox GL JS):
/// <code>
///   const firstSymbolId = map.getStyle().layers.find(l => l.type === 'symbol')?.id;
///   map.addLayer({
///     id: 'fiber-coverage-fill',
///     type: 'fill',
///     source: 'fiber-coverage',
///     paint: { 'fill-color': '#c026d3', 'fill-opacity': 0.35 }
///   }, firstSymbolId);  // ← insert BEFORE the first label layer
/// </code>
/// </summary>
public class GetFiberCoverageViewportHandler
    : IRequestHandler<GetFiberCoverageViewportRequest, ApiResponse<List<FiberCoverageViewportDto>>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository;

    public GetFiberCoverageViewportHandler(IBarangayCoverageRepository barangayCoverageRepository)
    {
        _barangayCoverageRepository = barangayCoverageRepository;
    }

    public async Task<ApiResponse<List<FiberCoverageViewportDto>>> Handle(
        GetFiberCoverageViewportRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Viewport.IsValid())
        {
            return new ApiResponse<List<FiberCoverageViewportDto>>
            {
                Succeeded = false,
                Message = "Invalid viewport bounds. Ensure MinLatitude < MaxLatitude and MinLongitude < MaxLongitude."
            };
        }

        var results = await _barangayCoverageRepository.GetFiberCoverageViewportAsync(
            request.Viewport,
            request.RegionIds,
            request.ProvinceIds,
            request.MunicipalityIds,
            cancellationToken);

        return new ApiResponse<List<FiberCoverageViewportDto>>
        {
            Data = results,
            Succeeded = true,
            Message = $"Retrieved {results.Count} fiber-covered barangay boundaries."
        };
    }
}
