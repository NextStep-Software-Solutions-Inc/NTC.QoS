using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Google.GenAI;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for Google Generative AI operations using official Google.GenAI SDK with Gemini models
/// </summary>
public class GenAIService : IGenAIService
{
    private readonly ILogger<GenAIService> _logger;
    private readonly Client _genAIClient;

    public GenAIService(IConfiguration configuration, ILogger<GenAIService> logger)
    {
        _logger = logger;
        var apiKey = configuration["GoogleAI:ApiKey"] 
            ?? throw new InvalidOperationException("GoogleAI:ApiKey is not configured. Please set it in user secrets or environment variables.");
        
        _genAIClient = new Client(apiKey: apiKey);
    }

    /// <summary>
    /// Sends a chat prompt to the AI model with optional file attachments
    /// </summary>
    public async Task<string> ChatAsync(string prompt, IFormFileCollection? files = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing chat request with prompt length: {PromptLength}, files: {FileCount}", 
                prompt.Length, files?.Count ?? 0);

            var enhancedPrompt = BuildPromptWithFileNote(prompt, files);

            // Use Gemini 3.0 Flash Preview (latest free tier model, fast and efficient)
            var response = await _genAIClient.Models.GenerateContentAsync(
                model: "gemini-3.0-flash-preview",
                contents: enhancedPrompt
            );
            
            var responseText = response?.Candidates?[0]?.Content?.Parts?[0]?.Text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Empty response received from AI model");
                return "I apologize, but I couldn't generate a response. Please try again.";
            }

            _logger.LogInformation("Chat response generated successfully");
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            throw new InvalidOperationException("Failed to process chat request. Please try again later.", ex);
        }
    }

    /// <summary>
    /// Generates a report using the AI model based on provided resources
    /// </summary>
    public async Task<string> GenerateReportAsync(string prompt, IFormFileCollection? files = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing report generation request with prompt length: {PromptLength}, files: {FileCount}", 
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

            enhancedPrompt = BuildPromptWithFileNote(enhancedPrompt, files);

            // Use Gemini 3.0 Flash Preview for report generation (latest free tier model)
            var response = await _genAIClient.Models.GenerateContentAsync(
                model: "gemini-3.0-flash-preview",
                contents: enhancedPrompt
            );
            
            var responseText = response?.Candidates?[0]?.Content?.Parts?[0]?.Text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Empty response received from AI model for report generation");
                return "Failed to generate report. Please try again with different parameters.";
            }

            _logger.LogInformation("Report generated successfully");
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            throw new InvalidOperationException("Failed to generate report. Please try again later.", ex);
        }
    }

    /// <summary>
    /// Helper method to add file note to prompt when files are present
    /// </summary>
    private string BuildPromptWithFileNote(string prompt, IFormFileCollection? files)
    {
        if (files != null && files.Count > 0)
        {
            _logger.LogInformation("File attachments present but multimodal processing not yet implemented");
            return prompt + $"\n\n[Note: {files.Count} file(s) were uploaded but cannot be processed in text-only mode. For full file analysis, multimodal API integration is required.]";
        }
        return prompt;
    }
}

