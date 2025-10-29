using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class WirelessValidation : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public Guid BarangayId { get; set; }
    public virtual Barangay? Barangay { get; set; }
    public Guid BandId { get; set; }
    public virtual Band? Band { get; set; }
    public Guid TechnologyId { get; set; }
    public virtual Technology? Technology { get; set; }
    public string? Address { get; set; } = null;
    public decimal? RSRP { get; set; }
    public bool IsMarginal { get; set; } = false;
}
