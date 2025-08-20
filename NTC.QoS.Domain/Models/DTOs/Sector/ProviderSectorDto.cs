namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class ProviderSectorDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
