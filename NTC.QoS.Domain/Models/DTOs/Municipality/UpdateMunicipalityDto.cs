using System.Text.Json;

namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class UpdateMunicipalityDto
{
    public string? Name { get; set; }
    public bool? IsCity { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; } 
    public JsonElement? Boundaries { get; set; }
    public float? Area { get; set; }
}
