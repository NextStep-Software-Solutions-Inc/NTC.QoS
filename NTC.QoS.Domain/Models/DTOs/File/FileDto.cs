namespace NTC.QoS.Domain.Models.DTOs.File;

public class FileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? RegionId { get; set; }
    public string? AssessmentJobId { get; set; } // Connectivity assessment job ID for status tracking
    public string? ErrorMessage { get; set; } // Human-readable error message or summary for user self-service
    public string? ErrorLogPath { get; set; } // Blob path or URL to detailed downloadable error log report
}
