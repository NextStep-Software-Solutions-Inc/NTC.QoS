namespace NTC.QoS.Domain.Models.DTOs.Province;

public class CreateProvinceDto
{
    public Guid RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
