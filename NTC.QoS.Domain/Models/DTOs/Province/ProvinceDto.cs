using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Domain.Models.DTOs.Province;

public class ProvinceDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public RegionDto? Region { get; set; }
    public string? Code { get; set; } = string.Empty;
    public float? Area { get; set; } = 0f;
}
