using NTC.QoS.Domain.Aggregates;
namespace NTC.QoS.Domain.Entities;

public class Technology : BaseEntity
{
    public Guid GenerationId { get; set; }
    public virtual Generation? Generation { get; set; }
    public string Standard { get; set; } = string.Empty;
    public string? Description { get; set; }
}
