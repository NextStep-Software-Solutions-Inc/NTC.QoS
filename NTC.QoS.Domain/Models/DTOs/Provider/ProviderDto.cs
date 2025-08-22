namespace NTC.QoS.Domain.Models.DTOs.Provider;

public class ProviderDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? LicenseNumber { get; set; } = string.Empty;
    public required string Color { get; set; }
    public bool HasLogo { get; set; }
    public string? LogoContentType { get; set; }
    public string? LogoFileName { get; set; }
    public long? LogoSizeBytes { get; set; }
    public DateTime? LogoUploadedAt { get; set; }
    public string? LogoDataBase64 { get; set; }
}
