using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Domain.Models.DTOs.Zone;

public class ZoneDto
{
    public Guid Id { get; set; }
    public Guid BarangayId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BarangayDto? Barangay { get; set; }

}
