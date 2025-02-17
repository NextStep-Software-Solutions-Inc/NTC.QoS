namespace NTC.QoS.Domain.Entities;

public class Zone : BaseEntity
{
    public Guid BarangayId { get; set; }
    public virtual Barangay? Barangay { get; set; }
    public string Name { get; set; } = string.Empty;
}


