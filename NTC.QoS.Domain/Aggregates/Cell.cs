using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Aggregates;

public class Cell : BaseEntity
{
    public Guid ZoneId { get; set; }
    public virtual Zone? Zone { get; set; }
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }
    public Guid? FileId { get; set; }
    public virtual NTC.QoS.Domain.Entities.File? File { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CID { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string TowerType { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public string TAC { get; set; } = string.Empty;
    public string? LAC { get; set; }
    public float CoorLatitude { get; set; }
    public float CoorLongitude { get; set; }
    public bool IsVerified { get; set; }
    public CellStatusEnum Status { get; set; } = CellStatusEnum.Unknown;

    public virtual List<Sector> Sectors { get; set; } = new();
    public virtual List<Speedtest> Speedtests { get; set; } = new();
    public virtual List<Cellular> Cellulars { get; set; } = new();


}
