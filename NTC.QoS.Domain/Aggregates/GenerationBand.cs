using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class GenerationBand : BaseEntity
{
    public Guid BandId { get; set; }
    public virtual Band? Band { get; set; }
    public Guid GenerationId { get; set; }
    public virtual Generation? Generation { get; set; }
}
