using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class NAP : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }
    public Guid? ZoneId { get; set; }
    public virtual Zone? Zone { get; set; }
    public Guid? FileId { get; set; }
    public virtual NTC.QoS.Domain.Entities.File? File { get; set; }
    public string CoorLatitude { get; set; } = string.Empty;
    public string CoorLongitude { get; set; } = string.Empty;
    public int? RowNumber { get; set; } 
}
