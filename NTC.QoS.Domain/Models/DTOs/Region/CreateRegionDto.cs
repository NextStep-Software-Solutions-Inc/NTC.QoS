using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.Region;

public class CreateRegionDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; } = string.Empty;
    public float? Area { get; set; } = 0f;
    public IFormFile? BoundariesFile { get; set; } = null;
}
