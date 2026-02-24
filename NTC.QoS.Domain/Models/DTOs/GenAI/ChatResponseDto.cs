namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Response DTO for AI chat assistant
/// </summary>
public class ChatResponseDto
{
    /// <summary>
    /// The AI-generated response
    /// </summary>
    public required string Response { get; set; }

    /// <summary>
    /// Timestamp of when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of files that were processed
    /// </summary>
    public int FilesProcessed { get; set; }
}
