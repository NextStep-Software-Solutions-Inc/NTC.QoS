using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;
using NTC.QoS.Domain.Models.DTOs.Common;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

/// <summary>
/// Request for retrieving barangay boundary polygons with wired FTTH fiber coverage data
/// within a map viewport bounding box (v2 API – viewport-optimized).
///
/// The response is intended for rendering the Fiber Networks coverage layer on the map.
/// The caller MUST add this GeoJSON layer before the map's symbol/label layers using
/// the <c>beforeId</c> parameter in MapLibre/Mapbox GL JS to prevent the overlay from
/// obscuring place-name labels.
/// </summary>
public class GetFiberCoverageViewportRequest : IRequest<ApiResponse<List<FiberCoverageViewportDto>>>
{
    /// <summary>
    /// Viewport bounding box used to filter barangays spatially.
    /// Only barangays whose boundaries intersect this box are returned.
    /// </summary>
    public required ViewportBounds Viewport { get; set; }

    /// <summary>
    /// Optional region filter (administrative level 1).
    /// </summary>
    public List<Guid>? RegionIds { get; set; }

    /// <summary>
    /// Optional province filter (administrative level 2).
    /// </summary>
    public List<Guid>? ProvinceIds { get; set; }

    /// <summary>
    /// Optional municipality filter (administrative level 3).
    /// </summary>
    public List<Guid>? MunicipalityIds { get; set; }
}
