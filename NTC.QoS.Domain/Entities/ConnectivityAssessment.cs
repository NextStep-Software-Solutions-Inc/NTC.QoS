using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents a connectivity assessment based on CMTS Broadband Mapping Rubric
/// </summary>
public class ConnectivityAssessment : BaseEntity, IGeoLocatedEntity
{
    /// <summary>
    /// Foreign key to the rubric configuration used for this assessment (nullable for backward compatibility)
    /// </summary>
    [ForeignKey(nameof(RubricConfiguration))]
    public Guid? RubricConfigurationId { get; set; }

    /// <summary>
    /// Version string of the rubric used for this assessment (for quick reference without join)
    /// </summary>
    public string? RubricVersion { get; set; }

    /// <summary>
    /// Navigation property to the rubric configuration used for this assessment
    /// </summary>
    public virtual RubricConfiguration? RubricConfiguration { get; set; }

    /// <summary>
    /// Location identifier (Barangay, Municipality, Province, or Region ID)
    /// </summary>
    public Guid LocationId { get; set; }

    /// <summary>
    /// Type of location being assessed
    /// </summary>
    public LocationTypeEnum LocationType { get; set; }

    /// <summary>
    /// Geographic center point of the assessed area
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// Area size in square kilometers
    /// </summary>
    public double AreaSizeKm2 { get; set; }

    /// <summary>
    /// Population count for the assessed area
    /// </summary>
    public int? Population { get; set; }

    /// <summary>
    /// Number of households in the assessed area
    /// </summary>
    public int? HouseholdCount { get; set; }

    /// <summary>
    /// Assessment date
    /// </summary>
    public DateTime AssessmentDate { get; set; }

    /// <summary>
    /// Overall connectivity status based on CMTS rubric
    /// </summary>
    public ConnectivityStatusEnum ConnectivityStatus { get; set; }

    /// <summary>
    /// Total score (0-15 based on CMTS rubric)
    /// </summary>
    public int TotalScore { get; set; }

    // CMTS Criteria Scores (3 = Served, 2 = Underserved, 1 = Unserved, 0 = No Data)

    /// <summary>
    /// Fiber Coverage Score (0-3 points)
    /// </summary>
    public int FiberCoverageScore { get; set; }

    /// <summary>
    /// Internet Access Speed Score (0-3 points)
    /// </summary>
    public int InternetAccessScore { get; set; }

    /// <summary>
    /// Technology Type Score (0-3 points)
    /// </summary>
    public int TechnologyTypeScore { get; set; }

    /// <summary>
    /// Quality of Service Score (0-3 points)
    /// </summary>
    public int QualityOfServiceScore { get; set; }

    /// <summary>
    /// Overall Coverage Availability Score (0-3 points)
    /// </summary>
    public int CoverageAvailabilityScore { get; set; }

    // Infrastructure Counts
    public int CellTowerCount { get; set; }
    public int FiberNAPCount { get; set; }
    public int WifiHotspotCount { get; set; }

    // Speed Test Statistics
    public decimal? AverageDownloadSpeedMbps { get; set; }
    public decimal? AverageUploadSpeedMbps { get; set; }
    public decimal? AverageLatencyMs { get; set; }
    public decimal? MedianDownloadSpeedMbps { get; set; }
    public decimal? MedianUploadSpeedMbps { get; set; }
    public int SpeedTestCount { get; set; }

    /// <summary>
    /// Average signal strength score (1-5) based on RSSI data
    /// 5 = Good, 4 = Average, 3 = Weak, 2 = Very Weak, 1 = No Broadband Coverage
    /// </summary>
    public double? AverageSignalStrength { get; set; }

    // Service Provider Information
    public int ServiceProviderCount { get; set; }
    public string? DominantServiceProvider { get; set; }

    /// <summary>
    /// Provider breakdown - detailed asset counts per provider
    /// Ordered by TotalAssetCount descending
    /// </summary>
    public virtual List<ConnectivityAssessmentProvider> Providers { get; set; } = [];

    // Coverage Percentages - Infrastructure Mix
    public double? MobileCoveragePercentage { get; set; }
    public double? FixedBroadbandCoveragePercentage { get; set; }
    public double? FiberCoveragePercentage { get; set; }

    // Population-Aware Coverage Metrics
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

    // Wireless Coverage Cache (Pre-computed for Report Performance)
    /// <summary>
    /// Pre-computed wireless coverage data stored as JSON.
    /// Contains BarangayWirelessSummaryDto with provider-level technology breakdown.
    /// Used to optimize wireless broadband report generation.
    /// </summary>
    public string? WirelessCoverageJson { get; set; }

    /// <summary>
    /// Timestamp when wireless coverage data was last computed and cached.
    /// Null if wireless coverage has never been computed for this location.
    /// </summary>
    public DateTime? WirelessCoverageLastUpdated { get; set; }

    // Additional Metrics
    public string? Notes { get; set; }
    public bool HasReliableConnectivity { get; set; }
    public bool MeetsMinimumSpeedRequirement { get; set; } // 25 Mbps download standard

    /// <summary>
    /// Date of last update to this assessment
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// User who performed or updated this assessment
    /// </summary>
    public string AssessedBy { get; set; } = string.Empty;
}