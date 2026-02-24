namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Response DTO for listing generated reports (summary view)
/// </summary>
public class GeneratedReportSummaryDto
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
    /// The prompt used (truncated for list view)
    /// </summary>
    public string PromptPreview { get; set; } = string.Empty;

    /// <summary>
    /// Number of files analyzed
    /// </summary>
    public int FilesAnalyzed { get; set; }

    /// <summary>
    /// When the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Content preview (first 200 characters)
    /// </summary>
    public string ContentPreview { get; set; } = string.Empty;
}
