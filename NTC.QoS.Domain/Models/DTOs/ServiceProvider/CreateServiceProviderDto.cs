namespace NTC.QoS.Domain.Models.DTOs.ServiceProvider;

public class CreateServiceProviderDto
{
    public Guid ProviderId { get; set; }
    public Guid ServiceId { get; set; }
    public string MNC { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;
}
