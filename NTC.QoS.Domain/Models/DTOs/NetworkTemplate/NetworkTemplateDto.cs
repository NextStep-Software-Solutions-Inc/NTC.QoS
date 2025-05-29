using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

public class NetworkTemplateDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    [JsonPropertyName("URL")]
    public string URL { get; set; } = string.Empty;
}
