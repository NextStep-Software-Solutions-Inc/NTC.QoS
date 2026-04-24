using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Connectivity Report DTO showing areas with rubric score breakdown, supporting unified view across all connectivity statuses
/// </summary>
public class ConnectivityReportDto
{
    public int TotalServedAreas { get; set; }
    public int ServedPopulation { get; set; }
    public int TotalUnservedAreas { get; set; }
    public int TotalUnderservedAreas { get; set; }
    public int UnservedPopulation { get; set; }
    public int UnderservedPopulation { get; set; }
    public List<ConnectivityAreaDetailDto> Areas { get; set; } = [];
    public ConnectivityScoreSummaryDto ScoreSummary { get; set; } = new();
    public List<GeographicLevelSummaryDto> MunicipalitySummaries { get; set; } = [];
    public List<GeographicLevelSummaryDto> ProvinceSummaries { get; set; } = [];
    public GeographicLevelSummaryDto? RegionSummary { get; set; }
    public int TotalAreas { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Detailed connectivity information for an area with full rubric breakdown
/// </summary>
public class ConnectivityAreaDetailDto
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? MunicipalityName { get; set; }
    public string? ProvinceName { get; set; }
    public ConnectivityStatusEnum ConnectivityStatus { get; set; }
    public int? Population { get; set; }
    
    // CMTS Rubric Scores (0-3 each)
    public int FiberCoverageScore { get; set; }
    public int InternetAccessScore { get; set; }
    public int TechnologyTypeScore { get; set; }
    public int QualityOfServiceScore { get; set; }
    public int CoverageAvailabilityScore { get; set; }
    
    // Total Score (0-15)
    public int TotalScore { get; set; }
    
    // Infrastructure Counts
    public int CellTowerCount { get; set; }
    public int FiberNAPCount { get; set; }
    public int WifiHotspotCount { get; set; }
    
    // Service Provider Info
    public int ServiceProviderCount { get; set; }
    public string? DominantServiceProvider { get; set; }
    
    // Speed Test Data
    public decimal? AverageDownloadSpeedMbps { get; set; }
    public decimal? AverageUploadSpeedMbps { get; set; }
    public int SpeedTestCount { get; set; }
    
    public DateTime LastAssessed { get; set; }
}

/// <summary>
/// Aggregated connectivity assessment summary for a single geographic level
/// (municipality, province, or region), derived from barangay-level data.
/// </summary>
public class GeographicLevelSummaryDto
{
    /// <summary>Name of the geographic unit (municipality, province, or region name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent geographic name.  For a municipality this is the province name;
    /// for a province this is the region name; empty for region level.
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>"Municipality", "Province", or "Region".</summary>
    public string Level { get; set; } = string.Empty;

    public int TotalBarangays { get; set; }
    public int ServedBarangays { get; set; }
    public int UnderservedBarangays { get; set; }
    public int UnservedBarangays { get; set; }

    public long TotalPopulation { get; set; }
    public long ServedPopulation { get; set; }
    public long UnderservedPopulation { get; set; }
    public long UnservedPopulation { get; set; }

    // Average rubric scores (0-3 each)
    public double AverageFiberCoverageScore { get; set; }
    public double AverageInternetAccessScore { get; set; }
    public double AverageTechnologyTypeScore { get; set; }
    public double AverageQualityOfServiceScore { get; set; }
    public double AverageCoverageAvailabilityScore { get; set; }
    public double AverageTotalScore { get; set; }

    // Score distribution
    public int ExcellentCount { get; set; } // 13-15
    public int GoodCount { get; set; }      // 11-12
    public int ModerateCount { get; set; }  // 8-10
    public int FairCount { get; set; }      // 6-7

    /// <summary>Derived overall status: Served (avg ≥ 11), Underserved (avg 6–10), Unserved (avg &lt; 6).</summary>
    public string OverallConnectivityStatus { get; set; } = string.Empty;
}

/// <summary>
/// Summary statistics for connectivity scores across all areas
/// </summary>
public class ConnectivityScoreSummaryDto
{
    // Average scores across all areas
    public double AverageFiberCoverageScore { get; set; }
    public double AverageInternetAccessScore { get; set; }
    public double AverageTechnologyTypeScore { get; set; }
    public double AverageQualityOfServiceScore { get; set; }
    public double AverageCoverageAvailabilityScore { get; set; }
    public double AverageTotalScore { get; set; }
    
    // Distribution counts by total score ranges
    public int ExcellentCount { get; set; } // 13-15 points
    public int GoodCount { get; set; } // 11-12 points
    public int ModerateCount { get; set; } // 8-10 points
    public int FairCount { get; set; } // 6-7 points
    
    // Total infrastructure
    public int TotalCellTowers { get; set; }
    public int TotalFiberNAPs { get; set; }
    public int TotalWifiHotspots { get; set; }
    public int TotalServiceProviders { get; set; }
}
