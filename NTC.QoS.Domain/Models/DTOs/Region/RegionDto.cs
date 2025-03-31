namespace NTC.QoS.Domain.Models.DTOs.Region;
public class RegionDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
