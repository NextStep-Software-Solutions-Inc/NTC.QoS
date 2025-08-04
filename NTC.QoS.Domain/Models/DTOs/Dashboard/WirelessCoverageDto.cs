namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class WirelessCoverageDto
{
    public string Technology { get; set; } = string.Empty;
    public decimal CoveragePercentage { get; set; }
    public int CoverageCount { get; set; }
    public int TotalRegionCount { get; set; }
}

public class RegionTechnologyCoverageDto
{
    public string Technology { get; set; } = string.Empty;
    public decimal CoveragePercentage { get; set; }
    public decimal AverageSignalStrength { get; set; }
    public int CoveragePointsCount { get; set; }
}

public class RegionWirelessCoverageDto
{
    public Guid RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public List<RegionTechnologyCoverageDto> TechnologiesCoverage { get; set; } = [];
    public decimal OverallCoveragePercentage { get; set; }
}

public class WirelessCoverageResponseDto
{
    public List<WirelessCoverageDto> OverallCoverage { get; set; } = [];
    public List<RegionWirelessCoverageDto> RegionsCoverage { get; set; } = [];
}
