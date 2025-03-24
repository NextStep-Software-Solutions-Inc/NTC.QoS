namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class ProviderSectorDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}
