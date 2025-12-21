namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Request DTO for AI chat assistant
/// </summary>
public class ChatRequestDto
{
    /// <summary>
    /// User's prompt or question
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Optional file attachments (images, PDFs, Excel, text files)
    /// </summary>
    public FileUpload[]? Files { get; set; }
}
