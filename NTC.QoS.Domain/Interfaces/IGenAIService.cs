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
}
