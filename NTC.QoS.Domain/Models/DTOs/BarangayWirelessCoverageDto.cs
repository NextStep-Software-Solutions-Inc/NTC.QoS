namespace NTC.QoS.Domain.Models.DTOs;

/// <summary>
/// Pre-computed wireless coverage data per barangay per provider.
/// Used to optimize wireless broadband report generation by caching provider-level technology breakdown.
/// Stored as JSON in ConnectivityAssessment.WirelessCoverageJson for performance.
/// </summary>
public class BarangayWirelessCoverageDto
{
    /// <summary>
    /// Barangay identifier
    /// </summary>
    public Guid BarangayId { get; set; }

    /// <summary>
    /// Provider identifier
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Provider name for display
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Technology availability flags (pre-computed from cellulars + validations)
    /// </summary>
    public bool Has2G { get; set; }
    public bool Has3G { get; set; }
    public bool Has4G { get; set; }
    public bool Has5G { get; set; }

    /// <summary>
    /// Technology counts (from cellular sites + validations within barangay)
    /// </summary>
    public int Count2G { get; set; }
    public int Count3G { get; set; }
    public int Count4G { get; set; }
    public int Count5G { get; set; }

    /// <summary>
    /// Total count of unique technologies (Count2G + Count3G + Count4G + Count5G)
    /// Matches TechnologyBandBreakdownDto.TotalTechnologyCount for scoring compatibility
    /// </summary>
    public int TotalTechnologyCount { get; set; }

    /// <summary>
    /// Ratio of modern tech (4G+5G) to total technologies (0.0 to 1.0)
    /// Matches TechnologyBandBreakdownDto.ModernCoverageRatio
    /// </summary>
    public double ModernCoverageRatio { get; set; }

    /// <summary>
    /// Whether provider has modern coverage (4G or 5G)
    /// Matches TechnologyBandBreakdownDto.HasModernCoverage
    /// </summary>
    public bool HasModernCoverage { get; set; }

    /// <summary>
    /// Whether legacy technologies (2G/3G) dominate over modern (4G/5G)
    /// Matches TechnologyBandBreakdownDto.IsLegacyDominated
    /// </summary>
    public bool IsLegacyDominated { get; set; }

    /// <summary>
    /// Dominant generation based on technology count
    /// Matches TechnologyBandBreakdownDto.DominantGeneration
    /// </summary>
    public string? DominantGeneration { get; set; }

    /// <summary>
    /// List of available generation names (e.g., ["4G", "5G"])
    /// Matches TechnologyBandBreakdownDto.AvailableGenerations
    /// </summary>
    public List<string> AvailableGenerations { get; set; } = new();

    /// <summary>
    /// Total cellular count for this provider in this barangay
    /// </summary>
    public int TotalCellularCount { get; set; }

    /// <summary>
    /// Wireless validation count for this provider in this barangay
    /// </summary>
    public int WirelessValidationCount { get; set; }

    /// <summary>
    /// Speedtest statistics for this provider in this barangay
    /// </summary>
    public int SpeedtestCount { get; set; }
    public decimal? AverageUploadSpeedMbps { get; set; }
    public decimal? AverageDownloadSpeedMbps { get; set; }
    public decimal? MedianUploadSpeedMbps { get; set; }
    public decimal? MedianDownloadSpeedMbps { get; set; }

    /// <summary>
    /// Median technology from speedtest data (e.g., "5G", "LTE", "3G")
    /// Used for "Tech Used" field in wireless reports
    /// </summary>
    public string? MedianTechnology { get; set; }

    /// <summary>
    /// Timestamp metadata
    /// </summary>
    public DateTime LastCellularUpdate { get; set; }
    public DateTime LastValidationUpdate { get; set; }
    public DateTime? LastSpeedtestUpdate { get; set; }
    public DateTime ComputedDate { get; set; }
}

/// <summary>
/// Summary of all wireless coverage data for a barangay (all providers combined)
/// Used for quick lookups in reports
/// </summary>
public class BarangayWirelessSummaryDto
{
    /// <summary>
    /// Barangay identifier
    /// </summary>
    public Guid BarangayId { get; set; }

    /// <summary>
    /// Total wireless providers with coverage in this barangay
    /// </summary>
    public int TotalWirelessProviders { get; set; }

    /// <summary>
    /// Total cellular sites across all providers
    /// </summary>
    public int TotalCellularCount { get; set; }

    /// <summary>
    /// Total wireless validations across all providers
    /// </summary>
    public int TotalWirelessValidationCount { get; set; }

    /// <summary>
    /// Total speedtests across all providers
    /// </summary>
    public int TotalSpeedtestCount { get; set; }

    /// <summary>
    /// Overall median technology across all providers
    /// </summary>
    public string? OverallMedianTechnology { get; set; }

    /// <summary>
    /// Best available technology in this barangay
    /// </summary>
    public string? BestTechnology { get; set; }

    /// <summary>
    /// Provider-level breakdown
    /// </summary>
    public List<BarangayWirelessCoverageDto> Providers { get; set; } = [];

    /// <summary>
    /// When this summary was computed
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
