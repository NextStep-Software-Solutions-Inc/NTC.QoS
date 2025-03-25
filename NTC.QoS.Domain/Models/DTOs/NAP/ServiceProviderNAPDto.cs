namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class ServiceProviderNAPDto
{
    public Guid Id { get; set; }
    public ProviderNAPDto? Provider { get; set; }
    public ServiceNAPDto? Service { get; set; }
}