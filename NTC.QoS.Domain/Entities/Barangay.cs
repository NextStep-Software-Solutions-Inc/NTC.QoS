using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Barangay : BaseEntity
{
    public Guid MunicipalityId { get; set; }
    public virtual Municipality? Municipality { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual ICollection<Speedtest>? Speedtests { get; set; }
    public virtual ICollection<Cellular>? Cellulars { get; set; }
    public virtual ICollection<Zone>? Zones { get; set; }

}


