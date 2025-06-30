using Microsoft.AspNetCore.Http;
namespace NTC.QoS.Domain.Models.DTOs.Province;

public class CreateProvinceDto
{
    public required Guid RegionId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string? Code { get; set; } = string.Empty;
    public IFormFile? BoundariesFile { get; set; }
    public float? Area { get; set; } = 0f;
}
