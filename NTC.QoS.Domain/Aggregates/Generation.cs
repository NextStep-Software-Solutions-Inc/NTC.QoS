using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class Generation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<Technology> Technologies { get; set; } = new();
    public virtual List<GenerationBand> GenerationBands { get; set; } = new();
    public virtual List<Speedtest> Speedtests { get; set; } = new();
    public virtual List<SystemType> SystemTypes { get; set; } = new();
}
