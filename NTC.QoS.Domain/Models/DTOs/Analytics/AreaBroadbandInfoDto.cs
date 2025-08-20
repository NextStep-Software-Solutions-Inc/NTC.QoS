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
}

public class BoundingBoxDto
{
    public double MinLatitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLongitude { get; set; }
}