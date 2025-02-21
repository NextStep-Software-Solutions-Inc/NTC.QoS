using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.ServiceProvider;

public class ServiceProviderDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public  ProviderDto? Provider { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public  ServiceDto? Service { get; set; } = null!;
    [JsonPropertyName ("MNC")]
    public string MNC { get; set; } = string.Empty;
    [JsonPropertyName("MCC")]
    public string MCC { get; set; } = string.Empty;

}