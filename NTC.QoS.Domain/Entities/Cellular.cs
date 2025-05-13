using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Cellular : BaseEntity
{
    public Guid CellId { get; set; }
    public virtual Cell? Cell { get; set; }
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }
    public Guid GenerationBandId { get; set; }
    public virtual GenerationBand? GenerationBand { get; set; }
    public string? Address { get; set; }
    public bool IsFileUploaded { get; set; }
}
