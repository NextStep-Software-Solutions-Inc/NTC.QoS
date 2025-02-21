using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Domain.Models.DTOs.ServiceProvider;

public class ServiceProviderDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public virtual ProviderDto Provider { get; set; } = new();
    public Guid ServiceId { get; set; }
    public virtual ServiceDto Service { get; set; } = new();
    public string MNC { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;

}