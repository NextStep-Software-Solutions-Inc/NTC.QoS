namespace NTC.QoS.Domain.Models.DTOs.Provider;

public class CreateProviderDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? LicenseNumber { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
}
