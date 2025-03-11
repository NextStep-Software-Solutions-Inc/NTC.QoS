using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

public class UpdateNetworkTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [JsonPropertyName("URL")]
    public string URL { get; set; } = string.Empty;
}
