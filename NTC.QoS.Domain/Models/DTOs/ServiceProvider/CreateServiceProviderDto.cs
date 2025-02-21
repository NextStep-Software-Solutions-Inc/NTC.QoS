namespace NTC.QoS.Domain.Models.DTOs.ServiceProvider;

public class CreateServiceProviderDto
{
    public int ProviderId { get; set; }
    public int ServiceID { get; set; }
    public string MNC { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;
}
