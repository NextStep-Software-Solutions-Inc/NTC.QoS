namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents provider breakdown data for a connectivity assessment
/// Stores individual provider asset counts within an assessed location
/// </summary>
public class ConnectivityAssessmentProvider : BaseEntity
{
    /// <summary>
    /// Foreign key to the connectivity assessment
    /// </summary>
    public Guid ConnectivityAssessmentId { get; set; }

    /// <summary>
    /// Navigation property to the connectivity assessment
    /// </summary>
    public virtual ConnectivityAssessment ConnectivityAssessment { get; set; } = null!;

    /// <summary>
    /// Provider name (e.g., "Globe Telecom", "Smart Communications")
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
    /// Number of wireless validations from this provider
    /// </summary>
    public int WirelessValidationCount { get; set; }

    /// <summary>
    /// Total assets from this provider (Cells + NAPs + WiFi + Wireless Validations)
    /// </summary>
    public int TotalAssetCount { get; set; }

    /// <summary>
    /// Percentage of total assets in the area from this provider
    /// </summary>
    public double AssetPercentage { get; set; }
}
