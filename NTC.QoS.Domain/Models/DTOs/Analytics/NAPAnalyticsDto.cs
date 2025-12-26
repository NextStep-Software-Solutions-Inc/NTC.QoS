using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Response DTO for NAP Analytics Dashboard
/// </summary>
public class NAPAnalyticsResponseDto
{
    /// <summary>
    /// Summary statistics for NAPs
    /// </summary>
    public NAPSummaryDto Summary { get; set; } = new();

    /// <summary>
    /// Market share distribution by provider
    /// </summary>
    public List<ProviderMarketShareDto> MarketShareDistribution { get; set; } = [];

    /// <summary>
    /// Provider comparison with NAP counts
    /// </summary>
    public List<ProviderComparisonDto> ProviderComparison { get; set; } = [];

    /// <summary>
    /// Leading provider information
    /// </summary>
    public LeadingProviderDto? LeadingProvider { get; set; }

    /// <summary>
    /// Geographic coverage bounds (min/max lat/long)
    /// </summary>
    public GeographicCoverageDto? GeographicCoverage { get; set; }

    /// <summary>
    /// Market analysis metrics
    /// </summary>
    public MarketAnalysisDto MarketAnalysis { get; set; } = new();
}

/// <summary>
/// Summary statistics for NAP analytics
/// </summary>
public class NAPSummaryDto
{
    /// <summary>
    /// Total number of NAPs
    /// </summary>
    public int TotalNAPs { get; set; }

    /// <summary>
    /// Number of active providers with NAPs
    /// </summary>
    public int ActiveProviders { get; set; }

    /// <summary>
    /// Average NAPs per provider
    /// </summary>
    public double AvgNAPsPerProvider { get; set; }

    /// <summary>
    /// Density (NAPs per square kilometer)
    /// </summary>
    public double DensityPerKm2 { get; set; }
}

/// <summary>
/// Provider market share information
/// </summary>
public class ProviderMarketShareDto
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Number of NAPs from this provider
    /// </summary>
    public int NAPCount { get; set; }

    /// <summary>
    /// Market share percentage
    /// </summary>
    public double MarketSharePercentage { get; set; }

    /// <summary>
    /// Provider color for UI display
    /// </summary>
    public string? ProviderColor { get; set; }
}

/// <summary>
/// Provider comparison data
/// </summary>
public class ProviderComparisonDto
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Number of NAPs
    /// </summary>
    public int NAPCount { get; set; }

    /// <summary>
    /// Provider color for UI display
    /// </summary>
    public string? ProviderColor { get; set; }
}

/// <summary>
/// Leading provider information
/// </summary>
public class LeadingProviderDto
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Market share percentage
    /// </summary>
    public double MarketSharePercentage { get; set; }

    /// <summary>
    /// Number of NAPs
    /// </summary>
    public int NAPCount { get; set; }
}

/// <summary>
/// Geographic coverage bounds
/// </summary>
public class GeographicCoverageDto
{
    /// <summary>
    /// Minimum latitude
    /// </summary>
    public double MinLatitude { get; set; }

    /// <summary>
    /// Maximum latitude
    /// </summary>
    public double MaxLatitude { get; set; }

    /// <summary>
    /// Minimum longitude
    /// </summary>
    public double MinLongitude { get; set; }

    /// <summary>
    /// Maximum longitude
    /// </summary>
    public double MaxLongitude { get; set; }

    /// <summary>
    /// Total coverage area in square kilometers
    /// </summary>
    public double TotalAreaKm2 { get; set; }

    /// <summary>
    /// Formatted coverage string for display
    /// </summary>
    public string CoverageString => $"{MinLatitude:F2}° - {MaxLatitude:F2}° N, {MinLongitude:F2}° - {MaxLongitude:F2}° E";
}

/// <summary>
/// Market analysis metrics
/// </summary>
public class MarketAnalysisDto
{
    /// <summary>
    /// Market concentration level
    /// </summary>
    public string Market { get; set; } = string.Empty;

    /// <summary>
    /// Competition score (0-100%)
    /// </summary>
    public int CompetitionScore { get; set; }

    /// <summary>
    /// Coverage quality rating
    /// </summary>
    public string Coverage { get; set; } = string.Empty;
}
