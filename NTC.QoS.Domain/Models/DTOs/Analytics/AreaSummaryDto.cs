namespace NTC.QoS.Domain.Models.DTOs.Analytics;

public class AreaSummaryDto
{
    public int TotalCells { get; set; }
    public int TotalNaps { get; set; }
    public int TotalAssets { get; set; }
    public double CoverageAreaKm2 { get; set; }
    public int ServiceProviderCount { get; set; }
    public int MunicipalityCount { get; set; }
    public double AssetDensityPerKm2 { get; set; }
}
