using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Controller for Google Generative AI features - Chat Assistant and Report Generation
/// </summary>
[ApiController]
[Route("genai", Name = "genai")]
[Authorize]
public class GenAIController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GenAIController> _logger;

    public GenAIController(IMediator mediator, ILogger<GenAIController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Chat with AI assistant
    /// </summary>
    /// <remarks>
    /// This endpoint allows users to interact with an AI assistant powered by Google's Gemini model.
    /// 
    /// **Features:**
    /// - Natural language Q&A about project data and outputs
    /// - Multi-modal support: accepts text prompts with optional file attachments
    /// - Supported file types: Images (JPEG, PNG, WebP, HEIC), PDFs, Excel files (.xlsx, .xls), and text files (.txt, .csv, .json)
    /// - Real-time conversational responses
    /// 
    /// **Usage Example:**
    /// - Ask questions about uploaded data: "What are the coverage statistics for Region 10?"
    /// - Analyze attached files: Upload a spreadsheet and ask "Summarize the key metrics in this file"
    /// - Get insights: "What patterns do you see in the network performance data?"
    /// 
    /// **Request Format:**
    /// Use `multipart/form-data` when including file attachments.
    /// 
    /// **Permissions Required:**
    /// - `genai.chat` - Allows access to the AI chat assistant
    /// 
    /// **Rate Limiting:**
    /// Subject to standard API rate limits and Google AI API quotas.
    /// </remarks>
    /// <param name="chatRequest">The chat request containing the prompt and optional files</param>
    /// <returns>AI-generated response</returns>
    /// <response code="200">Successfully generated chat response</response>
    /// <response code="400">Invalid request or AI service error</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    [HttpPost("chat")]
    [RequirePermission(Permissions.ChatWithAI)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), 400)]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> Chat([FromForm] ChatRequestDto chatRequest)
    {
        try
        {
            _logger.LogInformation("Received chat request with {FileCount} files", chatRequest.Files?.Count ?? 0);

            var request = new ChatRequest(chatRequest);
            var response = await _mediator.Send(request);

            return response.Succeeded ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return BadRequest(new ApiResponse<ChatResponseDto>
            {
                Succeeded = false,
                Message = "An error occurred while processing your request"
            });
        }
    }

    /// <summary>
    /// Generate a report using AI
    /// </summary>
    /// <remarks>
    /// This endpoint uses AI to generate comprehensive reports based on provided instructions and attached resources.
    /// 
    /// **Features:**
    /// - Automatic report generation from data files
    /// - Intelligent analysis and insight extraction
    /// - Professional formatting with executive summaries, key findings, and recommendations
    /// - Multi-file analysis: Combine data from multiple sources
    /// 
    /// **Supported File Types:**
    /// - Images: JPEG, PNG, WebP, HEIC (for visual data analysis)
    /// - Documents: PDF files
    /// - Spreadsheets: Excel (.xlsx, .xls) files
    /// - Text: Plain text (.txt), CSV, JSON files
    /// 
    /// **Usage Examples:**
    /// - Network Analysis: "Generate a coverage analysis report" + attach speedtest data
    /// - Performance Review: "Create a summary of network performance trends" + attach multiple data files
    /// - Custom Reports: "Compare the coverage between Region A and Region B" + attach regional data
    /// 
    /// **Report Structure:**
    /// The AI will generate reports containing:
    /// 1. Executive Summary
    /// 2. Detailed Analysis
    /// 3. Key Findings and Insights
    /// 4. Recommendations (when applicable)
    /// 
    /// **Request Format:**
    /// Use `multipart/form-data` when including file attachments.
    /// 
    /// **Permissions Required:**
    /// - `genai.generate_reports` - Allows generating AI-powered reports
    /// 
    /// **Performance:**
    /// Report generation time varies based on file size and complexity (typically 5-30 seconds).
    /// </remarks>
    /// <param name="reportRequest">The report generation request containing instructions and optional resource files</param>
    /// <returns>Generated report content</returns>
    /// <response code="200">Successfully generated report</response>
    /// <response code="400">Invalid request or AI service error</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    [HttpPost("generate-report")]
    [RequirePermission(Permissions.GenerateReports)]
    [ProducesResponseType(typeof(ApiResponse<GenerateReportResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<GenerateReportResponseDto>), 400)]
    public async Task<ActionResult<ApiResponse<GenerateReportResponseDto>>> GenerateReport([FromForm] GenerateReportRequestDto reportRequest)
    {
        try
        {
            _logger.LogInformation("Received report generation request with {FileCount} files", reportRequest.Files?.Count ?? 0);

            var request = new GenerateReportRequest(reportRequest);
            var response = await _mediator.Send(request);

            return response.Succeeded ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return BadRequest(new ApiResponse<GenerateReportResponseDto>
            {
                Succeeded = false,
                Message = "An error occurred while generating the report"
            });
        }
    }
}
