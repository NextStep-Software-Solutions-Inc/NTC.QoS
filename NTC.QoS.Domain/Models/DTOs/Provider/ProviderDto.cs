namespace NTC.QoS.Domain.Models.DTOs.Provider;

public class ProviderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LicenseNumber { get; set; }
}
