using NTC.QoS.Domain.Aggregates;
namespace NTC.QoS.Domain.Entities;

public class Technology : BaseEntity
{
    public Guid GenerationId { get; set; }
    public virtual Generation Generation { get; set; } = null!;
    public string Standard { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<SystemType> SystemTypes { get; set; } = new();
    public virtual List<TechnologyBand> TechnologyBands { get; set; } = new();
    public virtual List<Cellular> Cellulars { get; set; } = new();
}
