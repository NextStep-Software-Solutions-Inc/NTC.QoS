namespace NTC.QoS.Domain.Models.DTOs.GeoJson;

public class GeoJsonGeometryDto
{
    public required string Type { get; set; }
    public required List<List<List<double[]>>> Coordinates { get; set; }
}
