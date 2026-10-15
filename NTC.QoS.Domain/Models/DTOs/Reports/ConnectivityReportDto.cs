using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Connectivity Report DTO showing areas with rubric score breakdown, supporting unified view across all connectivity statuses.
/// Municipalities are the pagination unit; each municipality contains its full barangay list.
/// </summary>
public class ConnectivityReportDto
{
    // Top-level summary counts (all filtered barangays)
    public int TotalServedAreas { get; set; }
    public int ServedPopulation { get; set; }
    public int TotalUnservedAreas { get; set; }
    public int TotalUnderservedAreas { get; set; }
    public int UnservedPopulation { get; set; }
    public int UnderservedPopulation { get; set; }

    /// <summary>Paginated list of municipalities, each containing their full barangay list.</summary>
    public List<ConnectivityMunicipalityDto> Municipalities { get; set; } = [];

    public ConnectivityScoreSummaryDto ScoreSummary { get; set; } = new();
    public List<GeographicLevelSummaryDto> ProvinceSummaries { get; set; } = [];
    public GeographicLevelSummaryDto? RegionSummary { get; set; }

    // Pagination (municipality-level)
    public int TotalMunicipalities { get; set; }
    public int TotalBarangays { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// A municipality with aggregated connectivity statistics and its full nested barangay list.
/// </summary>
public class ConnectivityMunicipalityDto
{
    public Guid MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    public string ProvinceName { get; set; } = string.Empty;

    public int TotalBarangays { get; set; }
    public int ServedBarangays { get; set; }
    public int UnderservedBarangays { get; set; }
    public int UnservedBarangays { get; set; }

    public long TotalPopulation { get; set; }
    public long ServedPopulation { get; set; }
    public long UnderservedPopulation { get; set; }
    public long UnservedPopulation { get; set; }

    public double AverageFiberCoverageScore { get; set; }
    public double AverageInternetAccessScore { get; set; }
    public double AverageTechnologyTypeScore { get; set; }
    public double AverageQualityOfServiceScore { get; set; }
    public double AverageCoverageAvailabilityScore { get; set; }
    public double AverageTotalScore { get; set; }

    /// <summary>Derived overall status computed from average barangay CMTS scores: Served (≥11), Underserved (6–10), Unserved (&lt;6).</summary>
    public string OverallConnectivityStatus { get; set; } = string.Empty;

    /// <summary>Pre-stored status from the municipality-level connectivity assessment entity (null if not yet assessed).</summary>
    public string? AssessedConnectivityStatus { get; set; }

    /// <summary>Barangays within this municipality, sorted Unserved → Underserved → Served, then by name.</summary>
    public List<ConnectivityAreaDetailDto> Barangays { get; set; } = [];
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
    /// Parent geographic name when available in this DTO. For a municipality this is the province name;
    /// province and region summaries may leave this empty or null when no parent context is supplied.
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

    /// <summary>Derived overall status: Served (avg ≥ 11), Underserved (avg 6–10), Unserved (avg &lt; 6).</summary>
    public string OverallConnectivityStatus { get; set; } = string.Empty;

    /// <summary>
    /// Pre-computed status from the direct infrastructure assessment of this geographic unit.
    /// Null when no entity-level assessment has been run for this unit.
    /// </summary>
    public string? AssessedConnectivityStatus { get; set; }
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
    
    // Total infrastructure
    public int TotalCellTowers { get; set; }
    public int TotalFiberNAPs { get; set; }
    public int TotalWifiHotspots { get; set; }
    public int TotalServiceProviders { get; set; }
}
