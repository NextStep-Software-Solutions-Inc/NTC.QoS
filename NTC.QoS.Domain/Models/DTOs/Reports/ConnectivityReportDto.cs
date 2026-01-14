using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Connectivity Report DTO showing served areas with rubric score breakdown
/// </summary>
public class ConnectivityReportDto
{
    public int TotalServedAreas { get; set; }
    public int ServedPopulation { get; set; }
    public List<ConnectivityAreaDetailDto> Areas { get; set; } = [];
    public ConnectivityScoreSummaryDto ScoreSummary { get; set; } = new();
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
