using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class CellSectorDto
{
    public required Guid Id { get; set; }
    public required string SiteId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string TowerType { get; set; }
    public required ServiceProviderSectorDto ServiceProvider { get; set; }
}
