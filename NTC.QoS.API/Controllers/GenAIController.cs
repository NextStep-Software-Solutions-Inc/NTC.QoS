using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.API.Models;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models;
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
    /// Chat with AI assistant (Streaming Response)
    /// </summary>
    /// <remarks>
    /// This endpoint allows users to interact with an AI assistant powered by Google's Gemini model with real-time streaming responses.
    /// 
    /// **Features:**
    /// - Natural language Q&A about project data and outputs
    /// - **Streaming responses**: Real-time AI response chunks via Server-Sent Events (SSE)
    /// - **Multi-modal support**: Accepts text prompts with optional file attachments
    /// - **Supported file types**: 
    ///   - Images: JPEG, PNG, WebP, HEIC, GIF
    ///   - Documents: PDF files
    ///   - Text files: .txt, .csv, .json, .xml
    ///   - Spreadsheets: Excel (.xlsx, .xls) - extracted as text
    /// - File content is analyzed by the AI model for comprehensive responses
    /// 
    /// **Usage Examples:**
    /// - Ask questions: "What are the coverage statistics for Region 10?"
    /// - Analyze attached files: Upload a spreadsheet and ask "Summarize the key metrics in this file"
    /// - Multi-file analysis: Upload multiple images/PDFs and ask "Compare these documents"
    /// - Get insights: "What patterns do you see in the network performance data?"
    /// 
    /// **Request Format:**
    /// - Content-Type: `multipart/form-data` (required for file attachments)
    /// - Fields:
    ///   - `prompt` (required): Your question or instruction as text (max 10,000 characters)
    ///   - `files` (optional): One or more file attachments (max 10 files)
    /// 
    /// **Response Format:**
    /// - Content-Type: `text/event-stream` (Server-Sent Events)
    /// - Each chunk of the AI response is sent as it's generated
    /// - Connection stays open until the response is complete
    /// 
    /// **Permissions Required:**
    /// - `genai.chat` - Allows access to the AI chat assistant
    /// 
    /// **Rate Limiting:**
    /// Subject to standard API rate limits and Google AI API quotas.
    /// </remarks>
    /// <param name="chatRequest">The chat request containing the prompt and optional file attachments (max 10 files)</param>
    /// <returns>Streaming AI-generated response</returns>
    /// <response code="200">Successfully streaming chat response</response>
    /// <response code="400">Invalid request or AI service error</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    [HttpPost("chat")]
    [RequirePermission(Permissions.ChatWithAI)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), 400)]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> Chat([FromForm] ChatApiRequest apiRequest)
    {
        try
        {
            _logger.LogInformation("Received chat request with {FileCount} files", apiRequest.Files?.Length ?? 0);

            // Map from API request to domain DTO
            var chatRequest = new ChatRequestDto
            {
                Prompt = apiRequest.Prompt,
                Files = await MapFilesToDomainAsync(apiRequest.Files)
            };

            // Set headers for Server-Sent Events (SSE)
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var request = new ChatRequest(chatRequest, Response);
            await _mediator.Send(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming chat request");
            Response.StatusCode = 400;
            await Response.WriteAsync($"data: {{\"error\": \"An error occurred while processing your request: {ex.Message}\"}}\n\n");
        }
    }

    /// <summary>
    /// Generate a report using AI (Streaming Response)
    /// </summary>
    /// <remarks>
    /// This endpoint uses AI to generate comprehensive reports based on provided instructions and attached resources with real-time streaming.
    /// 
    /// **Features:**
    /// - **Streaming responses**: Real-time report generation via Server-Sent Events (SSE)
    /// - Automatic report generation from data files
    /// - Intelligent analysis and insight extraction
    /// - Professional formatting with executive summaries, key findings, and recommendations
    /// - **Multi-file analysis**: Combine data from multiple sources
    /// 
    /// **Supported File Types (with actual processing):**
    /// - Images: JPEG, PNG, WebP, HEIC, GIF (for visual data analysis)
    /// - Documents: PDF files (content extracted and analyzed)
    /// - Text: Plain text (.txt), CSV, JSON, XML files (content included in analysis)
    /// - Spreadsheets: Excel (.xlsx, .xls) files (extracted as text)
    /// 
    /// **Usage Examples:**
    /// - Network Analysis: "Generate a coverage analysis report" + attach speedtest data
    /// - Performance Review: "Create a summary of network performance trends" + attach multiple data files
    /// - Custom Reports: "Compare the coverage between Region A and Region B" + attach regional data
    /// - Visual Analysis: "Analyze these network topology diagrams" + attach images
    /// 
    /// **Report Structure:**
    /// The AI will generate reports containing:
    /// 1. Executive Summary
    /// 2. Detailed Analysis
    /// 3. Key Findings and Insights
    /// 4. Recommendations (when applicable)
    /// 
    /// **Request Format:**
    /// - Content-Type: `multipart/form-data` (required for file attachments)
    /// - Fields:
    ///   - `prompt` (required): Instructions for report generation (max 10,000 characters)
    ///   - `files` (optional): One or more resource files to analyze (max 10 files)
    /// 
    /// **Response Format:**
    /// - Content-Type: `text/event-stream` (Server-Sent Events)
    /// - Each chunk of the report is sent as it's generated
    /// - Connection stays open until the report is complete
    /// 
    /// **Permissions Required:**
    /// - `genai.generate_reports` - Allows generating AI-powered reports
    /// 
    /// **Performance:**
    /// Report generation time varies based on file size and complexity. Streaming provides real-time feedback.
    /// </remarks>
    /// <param name="reportRequest">The report generation request containing instructions and optional resource files (max 10 files)</param>
    /// <returns>Streaming generated report content</returns>
    /// <response code="200">Successfully streaming generated report</response>
    /// <response code="400">Invalid request or AI service error</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    [HttpPost("generate-report")]
    [RequirePermission(Permissions.GenerateReports)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ApiResponse<GenerateReportResponseDto>), 400)]
    public async Task<ActionResult<ApiResponse<GenerateReportResponseDto>>> GenerateReport([FromForm] GenerateReportApiRequest apiRequest)
    {
        try
        {
            _logger.LogInformation("Received report generation request with {FileCount} files", apiRequest.Files?.Length ?? 0);

            // Map from API request to domain DTO
            var reportRequest = new GenerateReportRequestDto
            {
                Prompt = apiRequest.Prompt,
                Files = await MapFilesToDomainAsync(apiRequest.Files)
            };

            // Set headers for Server-Sent Events (SSE)
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var request = new GenerateReportRequest(reportRequest, Response);
            await _mediator.Send(request);
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

        return fileUploads;
    }

    /// <summary>
    /// Maps IFormFile[] to domain FileUpload[] model
    /// </summary>
    private static async Task<FileUpload[]?> MapFilesToDomainAsync(IFormFile[]? files)
    {
        if (files == null || files.Length == 0)
            return null;

        var fileUploads = new FileUpload[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            fileUploads[i] = new FileUpload
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length,
                Content = memoryStream
            };
        }

        return fileUploads;
    }
}
