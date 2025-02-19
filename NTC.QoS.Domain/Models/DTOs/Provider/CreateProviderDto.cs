namespace NTC.QoS.Domain.Models.DTOs.Provider;

public class CreateProviderDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LicenseNumber { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
}
