using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

// Projection DTO used for speedtest query results
public class SpeedtestProjectionDto
{
    public Guid ServiceProviderId { get; set; }
    public decimal DownloadSpeed { get; set; }
    public ServiceTypeEnum ServiceType { get; set; }
}

// Projection DTO used for cellular bandwidth query results
public class CellularBandwidthProjectionDto
{
    public Guid ServiceProviderId { get; set; }
    public decimal Bandwidth { get; set; }
    public ServiceTypeEnum ServiceType { get; set; }
}

// Provider/info DTO used for cells and NAPs projections
public class ProviderInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public ServiceTypeEnum Type { get; set; }
}

// Asset projection for cells and NAPs including location and provider info
public class ProviderAssetDto
{
    public Guid Id { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Point Location { get; set; } = null!;
    public ProviderInfoDto ServiceProvider { get; set; } = new();
}
