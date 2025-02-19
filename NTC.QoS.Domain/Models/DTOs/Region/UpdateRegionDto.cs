namespace NTC.QoS.Domain.Models.DTOs.Region;

public class UpdateRegionDto 
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
