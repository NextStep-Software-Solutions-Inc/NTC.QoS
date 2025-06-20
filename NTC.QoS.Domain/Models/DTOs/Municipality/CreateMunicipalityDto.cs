using System.Text.Json;

namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class CreateMunicipalityDto
{
    public Guid ProvinceId { get; set; }
    public bool IsCity { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; } = string.Empty;
    public JsonElement? Boundaries { get; set; }
    public float? Area { get; set; } = 0f;
}
