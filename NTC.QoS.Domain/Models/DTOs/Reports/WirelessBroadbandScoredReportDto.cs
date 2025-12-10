using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Report;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Enhanced barangay wireless entry with connectivity scores
/// </summary>
public class BarangayWirelessScoredEntryDto
{
    public string MunicipalityName { get; set; } = string.Empty;
    public string BarangayName { get; set; } = string.Empty;

    /// <summary>
    /// Provider statistics per provider
    /// </summary>
    public Dictionary<string, ProviderWirelessStatsDto> ProviderStats { get; set; } = new();

    /// <summary>
    /// Overall connectivity scores for the barangay
    /// </summary>
    public WirelessConnectivityScoreDto ConnectivityScore { get; set; } = new();

    /// <summary>
    /// Overall service status based on scores
    /// </summary>
    public ServiceStatusEnum ServiceStatus { get; set; }

    /// <summary>
    /// Remarks about connectivity status
    /// </summary>
    public string Remarks { get; set; } = string.Empty;
}

/// <summary>
/// Wireless connectivity scoring based on CMTS criteria
/// </summary>
public class WirelessConnectivityScoreDto
{
    /// <summary>
    /// Technology Type Score (1-3 points)
    /// Based on: Fiber optic, LTE/5G, or high-speed DSL = 3
    ///           Legacy technologies (2G/3G, dial-up) = 2
    ///           No broadband technology = 1
    /// </summary>
    public int TechnologyTypeScore { get; set; }

    /// <summary>
    /// Internet Access Score (1-3 points) - from Speedtest data
    /// Based on: Speeds ≥25 Mbps = 3
    ///           Speeds available but slow/unreliable or <25 Mbps = 2
    ///           No internet access or minimal = 1
    /// </summary>
    public int InternetAccessScore { get; set; }

    /// <summary>
    /// Total score (sum of technology and internet access)
    /// 6 points = Served
    /// 4-5 points = Underserved
    /// 2-3 points = Unserved
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// Average download speed (Mbps) from speedtests
    /// </summary>
    public double? AverageDownloadSpeed { get; set; }

    /// <summary>
    /// Average upload speed (Mbps) from speedtests
    /// </summary>
    public double? AverageUploadSpeed { get; set; }

    /// <summary>
    /// Highest technology generation available (e.g., "5G", "4G")
    /// </summary>
    public string HighestTechnology { get; set; } = string.Empty;

    /// <summary>
    /// Explanation of score calculation
    /// </summary>
    public string ScoreBreakdown { get; set; } = string.Empty;
}

/// <summary>
/// Summary statistics for wireless broadband report with scores
/// </summary>
public class WirelessBroadbandScoredSummaryDto
{
    public int TotalMunicipalities { get; set; }
    public int TotalBarangays { get; set; }
    public int ServedBarangays { get; set; }
    public int UnderservedBarangays { get; set; }
    public int UnservedBarangays { get; set; }

    /// <summary>
    /// Average scores across all barangays
    /// </summary>
    public double AverageTechnologyScore { get; set; }
    public double AverageInternetAccessScore { get; set; }
    public double AverageTotalScore { get; set; }

    /// <summary>
    /// Coverage percentages
    /// </summary>
    public double ServedPercentage { get; set; }
    public double UnderservedPercentage { get; set; }
    public double UnservedPercentage { get; set; }

    /// <summary>
    /// Technology distribution
    /// </summary>
    public Dictionary<string, int> TechnologyDistribution { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}