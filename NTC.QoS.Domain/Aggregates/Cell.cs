using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Aggregates;

public class Cell : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string CID { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string TowerType { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public string TAC { get; set; } = string.Empty;
    public string? LAC { get; set; }
    public Point Location { get; set; } = null!;
    public bool IsVerified { get; set; }
    public CellStatusEnum Status { get; set; } = CellStatusEnum.Unknown;
    public bool IsFileUploaded { get; set; }

    public virtual List<CellSector> CellSectors { get; set; } = [];
    public virtual List<Cellular> Cellulars { get; set; } = [];
}
