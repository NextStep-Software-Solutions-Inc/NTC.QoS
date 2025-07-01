using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

public class CreateNetworkTemplateDto
{
    public required string Name { get; set; }
    [JsonPropertyName("URL")]
    public required string URL { get; set; }
    public string? Description { get; set; }
}
