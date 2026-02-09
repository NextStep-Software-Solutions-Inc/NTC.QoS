using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Request DTO for connectivity analysis
/// </summary>
public class ConnectivityAnalysisRequestDto
{
    /// <summary>
    /// Location IDs to analyze (Barangay, Municipality, Province, or Region IDs)
    /// </summary>
    public List<Guid> LocationIds { get; set; } = [];

    /// <summary>
    /// Type of locations being analyzed
    /// </summary>
    public LocationTypeEnum LocationType { get; set; }

    /// <summary>
    /// Filter by connectivity status (optional)
    /// </summary>
    public ConnectivityStatusEnum? FilterByStatus { get; set; }

    /// <summary>
    /// Include speed test data in analysis
    /// </summary>
    public bool IncludeSpeedTestData { get; set; } = true;

    /// <summary>
    /// Include infrastructure counts
    /// </summary>
    public bool IncludeInfrastructureData { get; set; } = true;
}

/// <summary>
/// Response DTO for connectivity analysis
/// </summary>
public class ConnectivityAnalysisResponseDto
{
    public LocationTypeEnum LocationType { get; set; }
    public List<LocationConnectivityDto> Locations { get; set; } = [];
    public ConnectivitySummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Individual location connectivity information
/// </summary>
public class LocationConnectivityDto
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public LocationTypeEnum LocationType { get; set; }
    public ConnectivityStatusEnum ConnectivityStatus { get; set; }
    public int TotalScore { get; set; }

    // CMTS Criteria Scores
    public CMTSScoresDto CMTSScores { get; set; } = new();

    // Infrastructure
    public InfrastructureStatsDto Infrastructure { get; set; } = new();

    // Speed Statistics
    public SpeedStatsDto? SpeedStats { get; set; }

    // Coverage Information
    public CoverageStatsDto Coverage { get; set; } = new();

    // Geographic Information
    public double AreaSizeKm2 { get; set; }
    public PointDto? CenterPoint { get; set; }
    public int? Population { get; set; }
    public int? HouseholdCount { get; set; }

    public DateTime AssessmentDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? Notes { get; set; }

    // Wireless Coverage (for barangays)
    public BarangayWirelessSummaryDto? WirelessCoverage { get; set; }
    public DateTime? WirelessCoverageLastUpdated { get; set; }
}

/// <summary>
/// CMTS subject/criterion names
/// </summary>
public static class CMTSSubjects
{
    public const string FiberCoverage = "Fiber Coverage";
    public const string InternetAccess = "Internet Access";
    public const string TechnologyType = "Technology Type";
    public const string QualityOfService = "Quality of Service";
    public const string MobileCoverage = "Mobile Coverage";
}

/// <summary>
/// Individual CMTS score item for radar chart visualization
/// </summary>
public class CMTSScoreItemDto
{
    /// <summary>
    /// Maximum score for CMTS criteria
    /// </summary>
    public const int MaxScore = 3;

    /// <summary>
    /// Subject/criterion name
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Actual score achieved (0-3).
    /// </summary>
    public int A { get; set; }

    /// <summary>
    /// Maximum possible score (always 3 for CMTS rubric)
    /// </summary>
    public int FullMark { get; set; } = MaxScore;
}

/// <summary>
/// CMTS rubric scores breakdown as array of score items
/// </summary>
public class CMTSScoresDto : List<CMTSScoreItemDto>
{
}

/// <summary>
/// Infrastructure statistics
/// </summary>
public class InfrastructureStatsDto
{
    public int CellTowerCount { get; set; }
    public int FiberNAPCount { get; set; }
    public int WiFiHotspotCount { get; set; }
    public int ServiceProviderCount { get; set; }
    public string? DominantServiceProvider { get; set; }

    /// <summary>
    /// Technology-band breakdown showing available bands for each technology standard,
    /// plus technology generation summary (counts, dominant generation, modern coverage ratio, etc.)
    /// Replaces the separate TechnologyBreakdown and BandBreakdown DTOs
    /// </summary>
    public TechnologyBandBreakdownDto? TechnologyBandBreakdown { get; set; }
    
    /// <summary>
    /// Detailed breakdown of service providers with their asset counts in descending order
    /// </summary>
    public List<ProviderBreakdownDto> ProviderBreakdown { get; set; } = [];
    public int TotalAssets => CellTowerCount + FiberNAPCount + WiFiHotspotCount;
}

/// <summary>
/// Provider breakdown showing asset counts per provider
/// </summary>
public class ProviderBreakdownDto
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Number of cell towers from this provider
    /// </summary>
    public int CellTowerCount { get; set; }

    /// <summary>
    /// Number of fiber NAPs from this provider
    /// </summary>
    public int FiberNAPCount { get; set; }

    /// <summary>
    /// Number of WiFi hotspots from this provider
    /// </summary>
    public int WiFiHotspotCount { get; set; }

    /// <summary>
    /// Total assets from this provider
    /// </summary>
    public int TotalAssetCount => CellTowerCount + FiberNAPCount + WiFiHotspotCount;
    
    /// <summary>
    /// Percentage of total assets in the area
    /// </summary>
    public double AssetPercentage { get; set; }
}

/// <summary>
/// Speed test statistics
/// </summary>
public class SpeedStatsDto
{
    public decimal? AverageDownloadSpeedMbps { get; set; }
    public decimal? AverageUploadSpeedMbps { get; set; }
    public decimal? AverageLatencyMs { get; set; }
    public decimal? MedianDownloadSpeedMbps { get; set; }
    public decimal? MedianUploadSpeedMbps { get; set; }
    public int SpeedTestCount { get; set; }
    public bool MeetsMinimumSpeedRequirement { get; set; } // 25 Mbps download

    /// <summary>
    /// Average signal strength score (1-5) based on RSSI data
    /// 5 = Good (40-70 dBm), 4 = Average (70-80 dBm), 3 = Weak (80-90 dBm), 
    /// 2 = Very Weak (90-100 dBm), 1 = No Broadband Coverage (100+ dBm)
    /// </summary>
    public double AverageSignalStrength { get; set; }

    /// <summary>
    /// Signal quality rating based on average signal strength
    /// </summary>
    public string SignalQualityRating => AverageSignalStrength switch
    {
        >= 4.5 => "Good",
        >= 3.5 => "Average",
        >= 2.5 => "Weak",
        >= 1.5 => "Very Weak",
        _ => "No Broadband Coverage"
    };
}

/// <summary>
/// Coverage statistics
/// </summary>
public class CoverageStatsDto
{
    public double? MobileCoveragePercentage { get; set; }
    public double? FixedBroadbandCoveragePercentage { get; set; }
    public double? FiberCoveragePercentage { get; set; }
    public bool HasReliableConnectivity { get; set; }

    // NEW: Population-aware coverage metrics
    /// <summary>
    /// Estimated percentage of population with broadband access
    /// </summary>
    public double? EstimatedPopulationCoveragePercent { get; set; }

    /// <summary>
    /// Estimated percentage of households with broadband access  
    /// </summary>
    public double? EstimatedHouseholdCoveragePercent { get; set; }

    /// <summary>
    /// Infrastructure assets per 1000 people
    /// </summary>
    public double? AssetsPerThousandPeople { get; set; }

    /// <summary>
    /// Service providers per thousand people
    /// </summary>
    public double? ProvidersPerThousandPeople { get; set; }
}

/// <summary>
/// Summary statistics for the entire analysis
/// </summary>
public class ConnectivitySummaryDto
{
    public int TotalLocations { get; set; }
    public int ServedCount { get; set; }
    public int UnderservedCount { get; set; }
    public int UnservedCount { get; set; }
    public double ServedPercentage => TotalLocations > 0 ? Math.Round((double)ServedCount / TotalLocations * 100, 2) : 0;
    public double UnderservedPercentage => TotalLocations > 0 ? Math.Round((double)UnderservedCount / TotalLocations * 100, 2) : 0;
    public double UnservedPercentage => TotalLocations > 0 ? Math.Round((double)UnservedCount / TotalLocations * 100, 2) : 0;

    public InfrastructureStatsDto TotalInfrastructure { get; set; } = new();
    public double TotalAreaKm2 { get; set; }
    public int? TotalPopulation { get; set; }
    public int? TotalHouseholds { get; set; }

    // Average scores across all locations
    public double AverageScore { get; set; }
    public CMTSScoresDto AverageCMTSScores { get; set; } = new();

    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
}