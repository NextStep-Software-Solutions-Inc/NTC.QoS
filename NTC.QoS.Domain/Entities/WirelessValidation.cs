namespace NTC.QoS.Domain.Entities;

public class WirelessValidation : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public Guid? BarangayId { get; set; }
    public Guid BandId { get; set; }
    public Guid TechnologyId { get; set; }
    public string? Address { get; set; } = string.Empty;
    public decimal RSSI { get; set; }
    public bool IsMarginal { get; set; } = false;
}
