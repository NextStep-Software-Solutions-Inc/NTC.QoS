using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Band : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<TechnologyBand> TechnologyBands { get; set; } = [];
    public virtual List<GenerationBand> GenerationBands { get; set; } = [];
    public virtual List<Cellular> Cellulars { get; set; } = [];
    public virtual List<Link> Links { get; set; } = [];
    public virtual List<WirelessValidation> WirelessValidations { get; set; } = [];
    public virtual List<Network> Networks { get; set; } = [];
}
