using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

public class AreaBroadbandInfoDto
{
    public AreaSummaryDto Summary { get; set; } = new();
    public List<ServiceProviderAreaInfoDto> ServiceProviders { get; set; } = [];
    public AreaEnvelopeInfoDto EnvelopeInfo { get; set; } = new();
    public List<MunicipalityAreaInfoDto> Municipalities { get; set; } = [];
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