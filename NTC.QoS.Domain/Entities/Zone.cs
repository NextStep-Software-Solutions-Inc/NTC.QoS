using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Zone : BaseEntity
{
    public Guid BarangayId { get; set; }
    public virtual Barangay Barangay { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public virtual List<Cell> Cells { get; set; } = new();
    public virtual List<NAP> NAPs { get; set; } = new();

}


