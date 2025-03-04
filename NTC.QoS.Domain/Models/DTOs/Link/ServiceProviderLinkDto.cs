using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Domain.Models.DTOs.Link;

public class ServiceProviderLinkDto
{
    public Guid Id { get; set; }
    public ProviderLinkDto? Provider { get; set; }
    public ServiceLinkDto? Service { get; set; }
}
