namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Response DTO for a single generated report
/// </summary>
public class GeneratedReportDto
{
    /// <summary>
    /// Unique identifier for the report
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Report title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The prompt used to generate the report
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// The generated report content in markdown format
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Number of files analyzed
    /// </summary>
    public int FilesAnalyzed { get; set; }

    /// <summary>
    /// File metadata (if available)
    /// </summary>
    public string? FileMetadata { get; set; }

    /// <summary>
    /// When the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// User who generated the report
    /// </summary>
    public string? GeneratedBy { get; set; }
}
