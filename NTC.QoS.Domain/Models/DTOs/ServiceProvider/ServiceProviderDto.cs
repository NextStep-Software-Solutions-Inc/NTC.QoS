using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.ServiceProvider;

public class ServiceProviderDto
{
    public Guid Id { get; set; }
    public required ProviderDto Provider { get; set; }
    public required ServiceDto Service { get; set; }
    [JsonPropertyName ("MNC")]
    public string MNC { get; set; } = string.Empty;
    [JsonPropertyName("MCC")]
    public string MCC { get; set; } = string.Empty;
}
