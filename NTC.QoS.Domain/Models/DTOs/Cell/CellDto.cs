using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class CellDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FileDto? File { get; set; }
    public ServiceProviderDto? ServiceProvider { get; set; }
    public ZoneDto? Zone { get; set; }
    public string CID { get; set; } = string.Empty;
    public string? LAC { get; set; }
    public string TAC { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string TowerType { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public required string CoorLatitude { get; set; }
    public required string CoorLongitude { get; set; }
    public bool IsVerified { get; set; }
    public string Status { get; set; } = string.Empty;
}
