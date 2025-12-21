namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Response DTO for AI report generation
/// </summary>
public class GenerateReportResponseDto
{
    /// <summary>
    /// The generated report content
    /// </summary>
    public required string ReportContent { get; set; }

    /// <summary>
    /// Timestamp of when the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of files that were analyzed
    /// </summary>
    public int FilesAnalyzed { get; set; }
}
