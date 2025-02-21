using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.ServiceProvider;

public class ServiceProviderDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public  ProviderDtos? Provider { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public  ServiceDtos? Service { get; set; } = null!;
    [JsonPropertyName ("MNC")]
    public string MNC { get; set; } = string.Empty;
    [JsonPropertyName("MCC")]
    public string MCC { get; set; } = string.Empty;

}
public class ProviderDtos
{
    public string? Name { get; set; } = string.Empty;
}
public class ServiceDtos
{
    public string? Name { get; set; } = string.Empty;
}