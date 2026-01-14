namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Request DTO for AI report generation
/// </summary>
public class GenerateReportRequestDto
{
    /// <summary>
    /// Instructions for the report generation
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Resource files to analyze for the report
    /// </summary>
    public FileUpload[]? Files { get; set; }
}
