namespace NTC.QoS.Domain.Models.DTOs.Provider;

public class ProviderLogoDto
{
    public byte[]? LogoData { get; set; } = null;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}