using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents a WiFi hotspot/access point for broadband coverage analysis
/// </summary>
public class WiFiHotspot : BaseEntity, IGeoLocatedEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Hotspot name/SSID
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the hotspot
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Geographic location
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// Coverage radius in meters
    /// </summary>
    public double CoverageRadiusMeters { get; set; }

    /// <summary>
    /// Whether the hotspot is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether the hotspot is currently operational
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Maximum bandwidth capacity in Mbps
    /// </summary>
    public decimal? MaxBandwidthMbps { get; set; }

    /// <summary>
    /// Operating frequency band (2.4GHz, 5GHz, etc.)
    /// </summary>
    public string? FrequencyBand { get; set; }

    /// <summary>
    /// Whether this was uploaded from a file
    /// </summary>
    public bool IsFileUploaded { get; set; } = false;

    /// <summary>
    /// Installation date
    /// </summary>
    public DateTime? InstallationDate { get; set; }

    /// <summary>
    /// Type of establishment (Mall, Government, School, etc.)
    /// </summary>
    public string? EstablishmentType { get; set; }

    /// <summary>
    /// Additional notes about the hotspot
    /// </summary>
    public string? Notes { get; set; }
}