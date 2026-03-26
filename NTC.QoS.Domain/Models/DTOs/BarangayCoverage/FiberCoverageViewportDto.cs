using NTC.QoS.Domain.Models.DTOs.GeoJson;

namespace NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

/// <summary>
/// Lightweight DTO for viewport-based fiber coverage map rendering (v2 API).
/// Returns barangay boundary polygons enriched with wired FTTH coverage data,
/// optimized for rendering the Fiber Networks coverage layer on the map.
///
/// IMPORTANT - Map Rendering Note:
/// This layer MUST be inserted before the map's label/symbol layers to prevent
/// the coverage overlay from obscuring place-name labels. In MapLibre/Mapbox GL JS
/// use the `beforeId` option when calling map.addLayer():
///
///   const firstSymbolId = map.getStyle().layers.find(l => l.type === 'symbol')?.id;
///   map.addLayer({ id: 'fiber-coverage', type: 'fill', source: ..., ... }, firstSymbolId);
/// </summary>
public class FiberCoverageViewportDto
{
    /// <summary>
    /// Barangay identifier
    /// </summary>
    public Guid BarangayId { get; set; }

    /// <summary>
    /// Barangay name for display
    /// </summary>
    public string BarangayName { get; set; } = string.Empty;

    /// <summary>
    /// Wired FTTH coverage percentage (0–100).
    /// Use this value to control fill-opacity or color intensity on the map layer.
    /// </summary>
    public decimal WiredFtthPercentage { get; set; }

    /// <summary>
    /// GeoJSON Feature representing the barangay boundary polygon.
    /// Null when boundary data is unavailable for the barangay.
    /// </summary>
    public GeoJsonFeatureDto? Boundaries { get; set; }
}
