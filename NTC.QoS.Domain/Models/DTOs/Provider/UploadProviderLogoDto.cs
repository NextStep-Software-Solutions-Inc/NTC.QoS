using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.Provider;

public class UploadProviderLogoDto
{
    public required IFormFile LogoFile { get; set; }
}