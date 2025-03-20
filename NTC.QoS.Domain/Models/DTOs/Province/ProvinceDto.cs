using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Domain.Models.DTOs.Province;
public class ProvinceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RegionDto? Region { get; set; }
}

