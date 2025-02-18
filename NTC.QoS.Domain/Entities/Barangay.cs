using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Barangay : BaseEntity
{
    public Guid MunicipalityId { get; set; }
    public virtual Municipality? Municipality { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<Speedtest> Speedtests { get; set; } = new();
    public virtual List<Cellular> Cellulars { get; set; } = new();
    public virtual List<Zone> Zones { get; set; } = new();

}


