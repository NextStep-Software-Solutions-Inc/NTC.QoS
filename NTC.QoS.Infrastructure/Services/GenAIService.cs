using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for Google Generative AI operations using Gemini models
/// </summary>
public class GenAIService : IGenAIService
{
    private readonly ILogger<GenAIService> _logger;
    private readonly string _apiKey;
    private readonly GoogleAI _googleAI;

    public GenAIService(IConfiguration configuration, ILogger<GenAIService> logger)
    {
        _logger = logger;
        _apiKey = configuration["GoogleAI:ApiKey"] 
            ?? throw new InvalidOperationException("GoogleAI:ApiKey is not configured. Please set it in user secrets or environment variables.");
        
        _googleAI = new GoogleAI(_apiKey);
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

            // Use Gemini 1.5 Flash (free tier, fast and efficient)
            var model = _googleAI.GenerativeModel(model: Model.Gemini15Flash);
            
            // For now, use text-only approach. File attachments would require multimodal handling
            // which may need a different API approach or the Vertex AI endpoint
            var enhancedPrompt = prompt;
            
            if (files != null && files.Count > 0)
            {
                enhancedPrompt += $"\n\n[Note: {files.Count} file(s) were uploaded. File processing is being implemented.]";
                _logger.LogWarning("File attachments received but not yet processed in this version");
            }

            var response = await model.GenerateContent(enhancedPrompt);
            
            if (response?.Text == null)
            {
                _logger.LogWarning("Empty response received from AI model");
                return "I apologize, but I couldn't generate a response. Please try again.";
            }

            _logger.LogInformation("Chat response generated successfully");
            return response.Text;
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

            // Use Gemini 1.5 Flash for report generation
            var model = _googleAI.GenerativeModel(model: Model.Gemini15Flash);
            
            var enhancedPrompt = $@"You are a professional report generator. Generate a comprehensive, well-structured report based on the following instructions and any attached files.

Instructions:
{prompt}

Please provide:
1. An executive summary
2. Detailed analysis based on the data provided
3. Key findings and insights
4. Recommendations (if applicable)

Format the report in a clear, professional manner with proper headings and sections.";

            if (files != null && files.Count > 0)
            {
                enhancedPrompt += $"\n\n[Note: {files.Count} file(s) were uploaded for analysis. File processing is being implemented.]";
                _logger.LogWarning("File attachments received but not yet processed in this version");
            }

            var response = await model.GenerateContent(enhancedPrompt);
            
            if (response?.Text == null)
            {
                _logger.LogWarning("Empty response received from AI model for report generation");
                return "Failed to generate report. Please try again with different parameters.";
            }

            _logger.LogInformation("Report generated successfully");
            return response.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            throw new InvalidOperationException("Failed to generate report. Please try again later.", ex);
        }
    }
}

