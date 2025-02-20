namespace NTC.QoS.Domain.Models.DTOs.Region;

public class CreateRegionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
