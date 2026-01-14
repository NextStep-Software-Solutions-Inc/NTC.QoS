namespace NTC.QoS.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Google Gemini AI models
/// </summary>
public class GeminiSettings
{
    /// <summary>
    /// The name of the chat model (e.g., gemini-2.0-flash-exp)
    /// </summary>
    public string ChatModel { get; set; } = "gemini-2.0-flash-exp";

    /// <summary>
    /// The name of the embedding model (e.g., text-embedding-004)
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-004";
}
