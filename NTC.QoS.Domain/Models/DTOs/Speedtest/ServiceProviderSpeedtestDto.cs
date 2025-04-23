using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class ServiceProviderSpeedtestDto
{
    public Guid Id { get; set; }
    public required ProviderDto Provider { get; set; }
    public required ServiceDto Service { get; set; }
}