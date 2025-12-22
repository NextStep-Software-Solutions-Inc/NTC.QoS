using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Google.GenAI;
using Google.GenAI.Types;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;
using System.Runtime.CompilerServices;
using NTC.QoS.Domain.Models;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for Google Generative AI operations using official Google.GenAI SDK with Gemini models
/// </summary>
public class GenAIService : IGenAIService
{
    private readonly ILogger<GenAIService> _logger;
    private readonly Client _genAIClient;
    private readonly GeminiSettings _settings;
    private readonly string _apiKey;

    public GenAIService(IOptions<GeminiSettings> settings, IConfiguration configuration, ILogger<GenAIService> logger)
    {
        _logger = logger;
        _settings = settings.Value;
        
        // Try environment variable first, then fall back to configuration
        _apiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
            ?? configuration["GEMINI_API_KEY"]
            ?? throw new InvalidOperationException("GEMINI_API_KEY is not configured. Please set it as an environment variable or in application configuration.");
        
        _genAIClient = new Client(apiKey: _apiKey);
    }

    /// <summary>
    /// Generates an embedding vector for the given text using the embedding model
    /// </summary>
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        _logger.LogInformation("Generating embedding for text of length: {TextLength}", text.Length);
        
        try
        {
            var response = await _genAIClient.Models.EmbedContentAsync(
                model: _settings.EmbeddingModel,
                contents: text);

            var embeddings = response.Embeddings;
            if (embeddings == null || embeddings.Count == 0 || embeddings[0].Values == null)
            {
                throw new InvalidOperationException("Embedding response did not contain valid values");
            }

            var values = embeddings[0].Values!;
            _logger.LogInformation("Successfully generated embedding with {Dimensions} dimensions", values.Count);
            // Convert double[] to float[] since Pgvector uses float
            return values.Select(v => (float)v).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw new InvalidOperationException("Failed to generate embedding. Please try again later.", ex);
        }
    }

    /// <summary>
    /// Generates content using the chat model (non-streaming)
    /// </summary>
    public async Task<string> GenerateContentAsync(string prompt)
    {
        _logger.LogInformation("Generating content for prompt of length: {PromptLength}", prompt.Length);
        
        try
        {
            var response = await _genAIClient.Models.GenerateContentAsync(
                model: _settings.ChatModel,
                contents: prompt);

            var text = response.Candidates?[0]?.Content?.Parts?[0]?.Text;
            if (string.IsNullOrEmpty(text))
            {
                throw new InvalidOperationException("Content generation did not return valid text");
            }

            _logger.LogInformation("Successfully generated content of length: {ContentLength}", text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content");
            throw new InvalidOperationException("Failed to generate content. Please try again later.", ex);
        }
    }

    /// <summary>
    /// Sends a chat prompt to the AI model with optional file attachments and streams the response
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string prompt, 
        FileUpload[]? files = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing streaming chat request with prompt length: {PromptLength}, files: {FileCount}", 
            prompt.Length, files?.Length ?? 0);

        var parts = new List<Part>();
        
        // Add text prompt
        parts.Add(new Part { Text = prompt });
        
        // Process file attachments
        if (files != null && files.Length > 0)
        {
            await foreach (var part in ProcessFilesAsync(files, cancellationToken))
            {
                parts.Add(part);
            }
        }

        // Stream the response
        await foreach (var chunk in StreamGenerateContentAsync(parts, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Generates a report using the AI model based on provided resources and streams the response
    /// </summary>
    public async IAsyncEnumerable<string> GenerateReportStreamAsync(
        string prompt, 
        FileUpload[]? files = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing streaming report generation request with prompt length: {PromptLength}, files: {FileCount}", 
            prompt.Length, files?.Length ?? 0);

        var enhancedPrompt = $@"Generate a comprehensive, well-structured report in clean markdown format based on the following instructions and any attached files. Output ONLY the report content with proper markdown headings and sections. Do not include any conversational text, meta-commentary, or explanations about the report - just the clean report itself.

{prompt}

Structure the report with:
1. An executive summary
2. Detailed analysis based on the data provided
3. Key findings and insights
4. Recommendations (if applicable)

Use proper markdown formatting with # for headings, ## for subheadings, - for bullet points, etc. The output should be ready for download or printing as a markdown document.";

        var parts = new List<Part>();
        
        // Add enhanced prompt
        parts.Add(new Part { Text = enhancedPrompt });
        
        // Process file attachments
        if (files != null && files.Length > 0)
        {
            await foreach (var part in ProcessFilesAsync(files, cancellationToken))
            {
                parts.Add(part);
            }
        }

        // Stream the response
        await foreach (var chunk in StreamGenerateContentAsync(parts, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Process uploaded files and convert them to appropriate parts for the AI model
    /// </summary>
    private async IAsyncEnumerable<Part> ProcessFilesAsync(
        FileUpload[] files, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            _logger.LogInformation("Processing file: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}", 
                file.FileName, file.Length, file.ContentType);

            Part? part = null;
            try
            {
                var fileBytes = await ReadFileAsync(file, cancellationToken);
                var mimeType = DetermineMimeType(file);

                // For images and PDFs, use InlineData for multimodal support
                if (IsImageFile(file.ContentType) || IsPdfFile(file.ContentType))
                {
                    part = new Part
                    {
                        InlineData = new Blob
                        {
                            MimeType = mimeType,
                            Data = fileBytes  // Already byte[], no need to convert
                        }
                    };
                    _logger.LogInformation("Added {FileType} file as InlineData: {FileName}", 
                        IsImageFile(file.ContentType) ? "image" : "PDF", file.FileName);
                }
                // For text-based files, extract text and add as Text part
                else if (IsTextBasedFile(file.ContentType, file.FileName))
                {
                    var textContent = System.Text.Encoding.UTF8.GetString(fileBytes);
                    part = new Part
                    { 
                        Text = $"\n\n--- Content from file: {file.FileName} ---\n{textContent}\n--- End of file ---\n" 
                    };
                    _logger.LogInformation("Added text-based file as Text part: {FileName}", file.FileName);
                }
                else
                {
                    _logger.LogWarning("Unsupported file type: {FileName}, ContentType: {ContentType}", 
                        file.FileName, file.ContentType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FileName}", file.FileName);
                // Continue processing other files
            }

            if (part != null)
            {
                yield return part;
            }
        }
    }

    /// <summary>
    /// Stream generate content from the AI model
    /// </summary>
    private async IAsyncEnumerable<string> StreamGenerateContentAsync(
        List<Part> parts,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var content = new Content { Parts = parts };
        
        IAsyncEnumerable<GenerateContentResponse>? stream = null;
        try
        {
            stream = _genAIClient.Models.GenerateContentStreamAsync(
                model: _settings.ChatModel,
                contents: content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing content stream");
            throw new InvalidOperationException("Failed to initialize AI response stream. Please try again later.", ex);
        }

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Stream cancelled by client");
                yield break;
            }

            var text = chunk?.Candidates?[0]?.Content?.Parts?[0]?.Text;
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }

        _logger.LogInformation("Streaming completed successfully");
    }

    /// <summary>
    /// Read file content as byte array
    /// </summary>
    private async Task<byte[]> ReadFileAsync(FileUpload file, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await file.Content.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Determine MIME type for the file
    /// </summary>
    private static string DetermineMimeType(FileUpload file)
    {
        // Use provided content type if available
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        // Fallback to extension-based detection
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Check if file is an image
    /// </summary>
    private static bool IsImageFile(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if file is a PDF
    /// </summary>
    private static bool IsPdfFile(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if file is text-based (including CSV, JSON, XML, TXT)
    /// </summary>
    private static bool IsTextBasedFile(string? contentType, string fileName)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            // Fallback to extension check
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension is ".txt" or ".csv" or ".json" or ".xml";
        }

        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("csv", StringComparison.OrdinalIgnoreCase);
    }
}

