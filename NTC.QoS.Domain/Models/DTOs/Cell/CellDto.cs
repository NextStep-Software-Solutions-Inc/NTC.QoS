using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class CellDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    public virtual FileDto File { get; set; } = new();
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProviderDto ServiceProvider { get; set; } = new();
    public Guid ZoneId { get; set; }
    public virtual ZoneDto Zone { get; set; } = new();
    public string CID { get; set; } = string.Empty;
    public string? LAC { get; set; }
    public string TAC { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string TowerType { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public string CoorLatitude { get; set; } = string.Empty;
    public string CoorLongitude { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ZoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
