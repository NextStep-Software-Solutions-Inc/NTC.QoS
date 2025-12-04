using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

public class AreaBroadbandInfoDto
{
    public AreaSummaryDto Summary { get; set; } = new();
    public List<ServiceProviderAreaInfoDto> ServiceProviders { get; set; } = [];
    public AreaEnvelopeInfoDto EnvelopeInfo { get; set; } = new();
    public List<MunicipalityAreaInfoDto> Municipalities { get; set; } = [];
    
    /// <summary>
    /// Location details including address information
    /// </summary>
    public LocationDetailsDto? LocationDetails { get; set; }
    
    /// <summary>
    /// Internet service availability and metrics
    /// </summary>
    public InternetServicesDto? InternetServices { get; set; }
    
    /// <summary>
    /// Signal strength information for cellular services
    /// </summary>
    public SignalStrengthDto? SignalStrength { get; set; }
    
    /// <summary>
    /// Service availability flags
    /// </summary>
    public ServiceAvailabilityDto ServiceAvailability { get; set; } = new();
}

public class ServiceProviderAreaInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public ServiceTypeEnum ServiceType { get; set; }
    public int CellCount { get; set; }
    public int NAPCount { get; set; }
    public int TotalAssets => CellCount + NAPCount;
    
    /// <summary>
    /// Maximum bandwidth/speed available from this provider in Mbps
    /// </summary>
    public decimal? MaxSpeedMbps { get; set; }
}

public class MunicipalityAreaInfoDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CellCount { get; set; }
    public int NAPCount { get; set; }
    public int TotalAssets => CellCount + NAPCount;

    /// <summary>
    /// Percentage of total assets in this municipality relative to the overall area
    /// </summary>
    public double AssetPercentage { get; set; }

    /// <summary>
    /// Indicates if this municipality has complete boundary data for accurate spatial calculations
    /// </summary>
    public bool HasBoundaryData => Id.HasValue;

    /// <summary>
    /// Indicates whether this municipality is covered by the query area (has boundaries that intersect)
    /// </summary>
    public bool IsCoveredByQuery { get; set; }

    /// <summary>
    /// Indicates whether this municipality has any broadband assets (cells or NAPs)
    /// </summary>
    public bool HasAssets => TotalAssets > 0;

    /// <summary>
    /// Coverage status of the municipality
    /// </summary>
    public MunicipalityCoverageStatus CoverageStatus =>
        HasAssets ? MunicipalityCoverageStatus.WithAssets :
        IsCoveredByQuery ? MunicipalityCoverageStatus.CoveredNoAssets :
        MunicipalityCoverageStatus.NotCovered;

    /// <summary>
    /// Display name for the municipality (handles "Unknown" case)
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Name) || Name == "Unknown"
        ? "Unknown Municipality"
        : Name;
}

public class AreaEnvelopeInfoDto
{
    public double AreaSizeKm2 { get; set; }
    public string GeometryType { get; set; } = string.Empty;
    public BoundingBoxDto BoundingBox { get; set; } = new();

    /// <summary>
    /// Center point of the query area (for circular queries)
    /// </summary>
    public PointDto? CenterPoint { get; set; }

    /// <summary>
    /// Radius in meters (for circular queries)
    /// </summary>
    public double? RadiusMeters { get; set; }
}

public class BoundingBoxDto
{
    public double MinLatitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLongitude { get; set; }
}

public class PointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>
/// Enumeration for municipality coverage status
/// </summary>
public enum MunicipalityCoverageStatus
{
    /// <summary>
    /// Municipality is not covered by the query area
    /// </summary>
    NotCovered = 0,

    /// <summary>
    /// Municipality is covered by query area but has no broadband assets
    /// </summary>
    CoveredNoAssets = 1,

    /// <summary>
    /// Municipality is covered by query area and has broadband assets
    /// </summary>
    WithAssets = 2
}

/// <summary>
/// Location details including address information
/// </summary>
public class LocationDetailsDto
{
    /// <summary>
    /// Street or road name (if available)
    /// </summary>
    public string? Street { get; set; }
    
    /// <summary>
    /// Barangay name
    /// </summary>
    public string? Barangay { get; set; }
    
    /// <summary>
    /// Municipality name
    /// </summary>
    public string? Municipality { get; set; }
    
    /// <summary>
    /// Province name
    /// </summary>
    public string? Province { get; set; }
    
    /// <summary>
    /// Region name
    /// </summary>
    public string? Region { get; set; }
}

/// <summary>
/// Internet services information
/// </summary>
public class InternetServicesDto
{
    /// <summary>
    /// Number of internet service providers in the area
    /// </summary>
    public int ProviderCount { get; set; }
    
    /// <summary>
    /// Maximum speed available from all providers in Mbps
    /// </summary>
    public decimal MaxSpeedMbps { get; set; }
}

/// <summary>
/// Signal strength information for cellular services
/// </summary>
public class SignalStrengthDto
{
    /// <summary>
    /// Average signal strength in dBm
    /// </summary>
    public decimal AverageStrengthDbm { get; set; }
    
    /// <summary>
    /// Maximum (strongest) signal strength in dBm
    /// </summary>
    public decimal MaxStrengthDbm { get; set; }
    
    /// <summary>
    /// Minimum (weakest) signal strength in dBm
    /// </summary>
    public decimal MinStrengthDbm { get; set; }
    
    /// <summary>
    /// Signal quality category: Excellent, Good, Fair, Weak, Poor
    /// </summary>
    public string Quality { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of signal strength measurements
    /// </summary>
    public int MeasurementCount { get; set; }
}

/// <summary>
/// Service availability flags
/// </summary>
public class ServiceAvailabilityDto
{
    /// <summary>
    /// Whether cellular service is available in the area
    /// </summary>
    public bool HasCellular { get; set; }
    
    /// <summary>
    /// <summary>
    /// Whether fiber service is available in the area.
    /// Note: This flag is derived from `ServiceTypeEnum.Wired` in handlers.
    /// Currently the system models only fiber as the wired service type.
    /// If other wired technologies (e.g., cable, DSL) are added later,
    /// consider renaming this property to `HasWired` or filtering specifically for fiber.
    /// Kept as `HasFiber` for now to match the existing data model and UI expectations.
    /// </summary>
    public bool HasFiber { get; set; }
}