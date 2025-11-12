namespace NTC.QoS.Domain.Models.DTOs.Analytics;

public class GeographicAnalysisDto
{
    public BoundingBoxDto BoundingBox { get; set; } = new();
    public CoverageHeatMapDto CoverageHeatMap { get; set; } = new();
}

public class CoverageHeatMapDto
{
    public List<DensityZoneDto> HighDensityZones { get; set; } = [];
    public List<DensityZoneDto> MediumDensityZones { get; set; } = [];
    public List<DensityZoneDto> LowDensityZones { get; set; } = [];
}

public class DensityZoneDto
{
    public Guid? MunicipalityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AssetDensity { get; set; }
    public string DominantServiceType { get; set; } = string.Empty;
    public bool UnderservedIndicator { get; set; }
    public CoordinateDto? Coordinates { get; set; }
}

public class CoordinateDto
{
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
}
