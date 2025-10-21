using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.File;

public class UploadFileDto
{
    public string? FileName { get; set; }
    public required string Type { get; set; }
    public required IFormFile File { get; set; }
    public Guid RegionId { get; set; }
}
