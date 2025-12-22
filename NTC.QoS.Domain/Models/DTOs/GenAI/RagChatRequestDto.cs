namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Request DTO for RAG-based chat with semantic context retrieval
/// </summary>
public class RagChatRequestDto
{
    /// <summary>
    /// User's message/question
    /// </summary>
    public required string Message { get; set; }
}
