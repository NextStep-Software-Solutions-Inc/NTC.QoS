using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Zone : BaseEntity
{
    public Guid BarangayId { get; set; }
    public virtual Barangay? Barangay { get; set; }
    public string Name { get; set; } = string.Empty;
    public virtual ICollection<Cell>? Cells { get; set; }
    public virtual ICollection<NAP>? NAPs { get; set; }

}


