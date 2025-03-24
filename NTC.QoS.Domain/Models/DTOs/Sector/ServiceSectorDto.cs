namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class ServiceSectorDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}
