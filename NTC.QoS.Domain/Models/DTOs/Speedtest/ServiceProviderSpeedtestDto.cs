namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class ServiceProviderSpeedtestDto
{
    public Guid Id { get; set; }
    public ProviderSpeedtestDto? Provider { get; set; }
    public ServiceSpeedtestDto? Service { get; set; }
}