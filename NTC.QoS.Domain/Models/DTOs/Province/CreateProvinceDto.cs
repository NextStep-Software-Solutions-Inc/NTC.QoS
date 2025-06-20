using System.Text.Json;

namespace NTC.QoS.Domain.Models.DTOs.Province;

public class CreateProvinceDto
{
    public Guid RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; } = string.Empty;
    public JsonElement? Boundaries { get; set; }
    public float? Area { get; set; } = 0f;
}
