namespace NTC.QoS.Domain.Models.DTOs.Zone;

public class CreateZoneDto
{
    public Guid BarangayId { get; set; }
    public string Name { get; set; } = string.Empty;
}
