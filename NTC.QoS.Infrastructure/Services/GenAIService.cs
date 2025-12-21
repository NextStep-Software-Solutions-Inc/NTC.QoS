using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Google.GenAI;
using Google.GenAI.Types;
using NTC.QoS.Domain.Interfaces;
using System.Runtime.CompilerServices;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for Google Generative AI operations using official Google.GenAI SDK with Gemini models
/// </summary>
public class GenAIService : IGenAIService
{
    private readonly ILogger<GenAIService> _logger;
    private readonly Client _genAIClient;
    private readonly string _modelName = "gemini-2.0-flash-exp";

    public GenAIService(IConfiguration configuration, ILogger<GenAIService> logger)
    {
        _logger = logger;
        var apiKey = configuration["GoogleAI:ApiKey"] 
            ?? throw new InvalidOperationException("GoogleAI:ApiKey is not configured. Please set it in user secrets or environment variables.");
        
        _genAIClient = new Client(apiKey: apiKey);
    }

    /// <summary>
    /// Sends a chat prompt to the AI model with optional file attachments and streams the response
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string prompt, 
        IFormFileCollection? files = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing streaming chat request with prompt length: {PromptLength}, files: {FileCount}", 
            prompt.Length, files?.Count ?? 0);

        var parts = new List<Part>();
        
        // Add text prompt
        parts.Add(new Part { Text = prompt });
        
        // Process file attachments
        if (files != null && files.Count > 0)
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
        IFormFileCollection? files = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing streaming report generation request with prompt length: {PromptLength}, files: {FileCount}", 
            prompt.Length, files?.Count ?? 0);

        var enhancedPrompt = $@"You are a professional report generator. Generate a comprehensive, well-structured report based on the following instructions and any attached files.

Instructions:
{prompt}

Please provide:
1. An executive summary
2. Detailed analysis based on the data provided
3. Key findings and insights
4. Recommendations (if applicable)

Format the report in a clear, professional manner with proper headings and sections.";

        var parts = new List<Part>();
        
        // Add enhanced prompt
        parts.Add(new Part { Text = enhancedPrompt });
        
        // Process file attachments
        if (files != null && files.Count > 0)
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
        IFormFileCollection files, 
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
                model: _modelName,
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
    private async Task<byte[]> ReadFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Determine MIME type for the file
    /// </summary>
    private string DetermineMimeType(IFormFile file)
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
    private bool IsImageFile(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if file is a PDF
    /// </summary>
    private bool IsPdfFile(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if file is text-based (including CSV, JSON, XML, TXT)
    /// </summary>
    private bool IsTextBasedFile(string? contentType, string fileName)
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

