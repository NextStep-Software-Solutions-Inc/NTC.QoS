using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;
using NTC.QoS.Domain.Models.DTOs.Common;

namespace NTC.QoS.Domain.Interfaces;

public interface IBarangayCoverageRepository : IRepository<BarangayCoverage>
{
    /// <summary>
    /// Get coverage data for a specific barangay
    /// </summary>
    Task<BarangayCoverage?> GetByBarangayIdAsync(Guid barangayId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all coverage data for barangays in a specific region
    /// </summary>
    Task<List<BarangayCoverage>> GetByRegionIdAsync(Guid regionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get coverage data by ID with full relationships (excluding boundaries)
    /// </summary>
    Task<BarangayCoverage?> GetByIdWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all coverage data with optional filters, lightweight without relationships
    /// </summary>
    Task<List<BarangayCoverage>> GetAllWithFiltersAsync(
        List<Guid>? regionIds = null,
        List<Guid>? provinceIds = null,
        List<Guid>? municipalityIds = null,
        List<Guid>? barangayIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get barangay boundary polygons with wired FTTH fiber coverage data within a
    /// map viewport bounding box (v2 API – viewport-optimized).
    ///
    /// Only barangays with <c>WiredFtthPercentage &gt; 0</c> whose boundaries
    /// intersect the viewport are returned.  The data is intended for rendering
    /// the Fiber Networks coverage layer as a GeoJSON fill layer on the map.
    ///
    /// To avoid obscuring place-name labels the caller MUST insert the layer before
    /// the first symbol/label layer in the active map style (use <c>beforeId</c>
    /// in MapLibre/Mapbox GL JS).
    /// </summary>
    Task<List<FiberCoverageViewportDto>> GetFiberCoverageViewportAsync(
        ViewportBounds viewport,
        List<Guid>? regionIds = null,
        List<Guid>? provinceIds = null,
        List<Guid>? municipalityIds = null,
        CancellationToken cancellationToken = default);
}
