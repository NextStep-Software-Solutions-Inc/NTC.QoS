namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class ServiceProviderSectorDto
{
    public Guid Id { get; set; }
    public required ProviderSectorDto Provider { get; set; }
    public required ServiceSectorDto Service { get; set; }
}
