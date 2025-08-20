using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class TechnologyBand : BaseEntity
{
    public Guid BandId { get; set; }
    public virtual Band Band { get; set; } = null!;
    public Guid TechnologyId { get; set; }
    public virtual Technology Technology { get; set; } = null!;
}