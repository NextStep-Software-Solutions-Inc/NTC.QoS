namespace NTC.QoS.API.Models;

/// <summary>
/// API-level request for RAG-based chat with context retrieval
/// </summary>
public class RagChatApiRequest
{
    /// <summary>
    /// User's message/question
    /// </summary>
    public required string Message { get; set; }
}
