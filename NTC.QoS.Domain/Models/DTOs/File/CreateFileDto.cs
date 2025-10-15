using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.File;

public class CreateFileDto
{
    public required string Type { get; set; }
    public string? FileName { get; set; }
    public required IFormFile File { get; set; }
    
    /// <summary>
    /// Optional region ID for the file upload. If not specified, 
    /// the system will use the user's first accessible region.
    /// </summary>
    public Guid? RegionId { get; set; }
}