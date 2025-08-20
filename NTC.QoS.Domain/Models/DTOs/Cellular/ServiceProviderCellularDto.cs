namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class ServiceProviderCellularDto
{
    public Guid Id { get; set; }
    public CommonCellularDto? Provider { get; set; }
    public ServiceCellularDto? Service { get; set; }
}
