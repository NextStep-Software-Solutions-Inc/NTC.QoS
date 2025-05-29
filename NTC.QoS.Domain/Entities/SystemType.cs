using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class SystemType : BaseEntity
{
    public Guid TechnologyId { get; set; }
    public virtual Technology? Technology { get; set; }   
    public string Name { get; set; } = string.Empty;
    public virtual List<Network> Networks { get; set; } = new();
}
