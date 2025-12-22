using NTC.QoS.Domain.Models;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for Google Generative AI operations
/// </summary>
public interface IGenAIService
{
    /// <summary>
    /// Sends a chat prompt to the AI model with optional file attachments and streams the response
    /// </summary>
    /// <param name="prompt">The user's prompt/question</param>
    /// <param name="files">Optional file attachments (images, PDFs, Excel, text files)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable stream of response chunks</returns>
    IAsyncEnumerable<string> ChatStreamAsync(string prompt, FileUpload[]? files = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a report using the AI model based on provided resources and streams the response
    /// </summary>
    /// <param name="prompt">Instructions for report generation</param>
    /// <param name="files">Resource files to analyze for the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable stream of response chunks</returns>
    IAsyncEnumerable<string> GenerateReportStreamAsync(string prompt, FileUpload[]? files = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding vector for the given text using the embedding model
    /// </summary>
    /// <param name="text">The text to embed</param>
    /// <returns>A float array representing the embedding vector</returns>
    Task<float[]> GetEmbeddingAsync(string text);

    /// <summary>
    /// Generates content using the chat model (non-streaming)
    /// </summary>
    /// <param name="prompt">The prompt to generate content from</param>
    /// <returns>The generated content as a string</returns>
    Task<string> GenerateContentAsync(string prompt);

    /// <summary>
    /// Generates content using the chat model with streaming response
    /// </summary>
    /// <param name="prompt">The prompt to generate content from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable stream of response chunks</returns>
    IAsyncEnumerable<string> GenerateContentStreamAsync(string prompt, CancellationToken cancellationToken = default);
}
