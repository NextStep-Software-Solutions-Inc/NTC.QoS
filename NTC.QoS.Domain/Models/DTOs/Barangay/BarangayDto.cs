using NTC.QoS.Domain.Models.DTOs.GeoJson;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Domain.Models.DTOs.Barangay;

public class BarangayDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; } = string.Empty;
    public MunicipalityDto? Municipality { get; set; }
    public string? Code { get; set; } = string.Empty;
    public float? Area { get; set; }
    // GeoJSON Feature representing barangay boundaries
    public GeoJsonFeatureDto? Boundaries { get; set; }
}
