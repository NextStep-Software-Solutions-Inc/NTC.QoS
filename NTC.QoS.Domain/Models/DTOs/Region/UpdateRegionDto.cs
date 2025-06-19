using System.Text.Json;

namespace NTC.QoS.Domain.Models.DTOs.Region;

public class UpdateRegionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; } = string.Empty;
    public float? Area { get; set; } = 0f;
    public JsonElement? Boundaries { get; set; }
}
