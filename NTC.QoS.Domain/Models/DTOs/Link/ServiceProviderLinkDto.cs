namespace NTC.QoS.Domain.Models.DTOs.Link;

public class ServiceProviderLinkDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
}
