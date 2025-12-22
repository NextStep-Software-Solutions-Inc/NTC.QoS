namespace NTC.QoS.Domain.Models.DTOs.GenAI;

/// <summary>
/// Represents a chat message for session history
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The role of the message sender (user or assistant)
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// The content of the message
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// The timestamp of the message
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
