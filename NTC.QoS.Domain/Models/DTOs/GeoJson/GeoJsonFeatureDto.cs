namespace NTC.QoS.Domain.Models.DTOs.GeoJson;

public class GeoJsonFeatureDto
{
    public string Type { get; set; } = "Feature";
    public required GeoJsonGeometryDto Geometry { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
