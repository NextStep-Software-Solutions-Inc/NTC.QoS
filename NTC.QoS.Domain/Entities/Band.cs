using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;


public class Band : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<Link> Links { get; set; } = new();
    public virtual List<GenerationBand> GenerationBands { get; set; } = new();

}
