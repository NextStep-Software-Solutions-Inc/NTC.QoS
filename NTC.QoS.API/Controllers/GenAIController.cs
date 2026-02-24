using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.API.Models;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
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
    private readonly IMarkdownToPdfService _markdownToPdfService;

    public GenAIController(IMediator mediator, ILogger<GenAIController> logger, IMarkdownToPdfService markdownToPdfService)
    {
        _mediator = mediator;
        _logger = logger;
        _markdownToPdfService = markdownToPdfService;
    }

    /// <summary>
    /// Chat with AI assistant (Streaming Response with RAG)
    /// </summary>
    /// <remarks>
    /// This endpoint allows users to interact with an AI assistant powered by Google's Gemini model with real-time streaming responses.
    /// Uses RAG (Retrieval Augmented Generation) to provide context-aware responses based on previously generated reports.
    /// 
    /// **Features:**
    /// - **RAG-based responses**: Automatically retrieves relevant context from previously generated reports
    /// - **Session management**: Maintains chat history per user (expires after 30 minutes of inactivity)
    /// - **Semantic search**: Uses vector embeddings to find the most relevant reports for context
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
    /// **How RAG works:**
    /// 1. Your message is converted to a vector embedding
    /// 2. The system searches for the 3 most semantically similar generated reports
    /// 3. These reports are used as context for generating a response
    /// 4. Chat history is maintained across messages in the same session
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
    public async Task Chat([FromForm] ChatApiRequest apiRequest)
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
    /// Generate a report using AI (Streaming Response with RAG)
    /// </summary>
    /// <remarks>
    /// This endpoint uses AI to generate comprehensive reports based on provided instructions and attached resources with real-time streaming.
    /// Uses RAG (Retrieval Augmented Generation) to leverage context from previously generated reports.
    /// 
    /// **Features:**
    /// - **RAG-based generation**: Automatically retrieves relevant context from previously generated reports
    /// - **Streaming responses**: Real-time report generation via Server-Sent Events (SSE)
    /// - **Semantic search**: Uses vector embeddings to find related reports for enhanced context
    /// - Automatic report generation from data files
    /// - Intelligent analysis and insight extraction
    /// - Professional formatting with executive summaries, key findings, and recommendations
    /// - **Multi-file analysis**: Combine data from multiple sources
    /// - Generated reports are saved with embeddings for future RAG retrieval
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
    public async Task GenerateReport([FromForm] GenerateReportApiRequest apiRequest)
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
            Response.StatusCode = 400;
            await Response.WriteAsync($"data: {{\"error\": \"An error occurred while generating the report: {ex.Message}\"}}\n\n");
        }
    }

    /// <summary>
    /// Get all generated reports for the current user with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of generated report summaries</returns>
    /// <response code="200">Successfully retrieved reports</response>
    /// <response code="401">Unauthorized - authentication required</response>
    [HttpGet("reports")]
    [RequirePermission(Permissions.GenerateReports)]
    [ProducesResponseType(typeof(PaginatedResponse<GeneratedReportSummaryDto>), 200)]
    public async Task<ActionResult<PaginatedResponse<GeneratedReportSummaryDto>>> GetReports(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetUserReportsQuery { PageNumber = pageNumber, PageSize = pageSize };
        var response = await _mediator.Send(query);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Get a specific generated report by ID
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <returns>Complete report details including content</returns>
    /// <response code="200">Successfully retrieved report</response>
    /// <response code="404">Report not found</response>
    /// <response code="401">Unauthorized - authentication required</response>
    [HttpGet("reports/{id}")]
    [RequirePermission(Permissions.GenerateReports)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedReportDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<GeneratedReportDto>), 404)]
    public async Task<ActionResult<ApiResponse<GeneratedReportDto>>> GetReportById(Guid id)
    {
        var query = new GetReportByIdQuery(id);
        var response = await _mediator.Send(query);
        return response.Succeeded ? Ok(response) : NotFound(response);
    }

    /// <summary>
    /// Download a generated report as a markdown or PDF file
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <param name="format">Report format: markdown (default) or pdf</param>
    /// <returns>Markdown or PDF file download</returns>
    /// <response code="200">Successfully downloaded report</response>
    /// <response code="404">Report not found</response>
    /// <response code="401">Unauthorized - authentication required</response>
    [HttpGet("reports/{id}/download")]
    [RequirePermission(Permissions.GenerateReports)]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> DownloadReport(Guid id, [FromQuery] string format = "markdown")
    {
        var query = new GetReportByIdQuery(id);
        var response = await _mediator.Send(query);

        if (!response.Succeeded || response.Data == null)
        {
            return NotFound(response);
        }

        var isPdf = format.Equals("pdf", StringComparison.OrdinalIgnoreCase);
        var extension = isPdf ? "pdf" : "md";
        var contentType = isPdf ? "application/pdf" : "text/markdown";

        var fileName = !string.IsNullOrWhiteSpace(response.Data.Title)
            ? $"{response.Data.Title}.{extension}"
            : $"Report_{response.Data.GeneratedAt:yyyyMMdd_HHmmss}.{extension}";

        // Sanitize filename
        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

        byte[] fileBytes;
        
        if (isPdf)
        {
            // Convert markdown content to PDF using the dedicated service
            try
            {
                var cleanedContent = StripOuterMarkdownFence(response.Data.Content);
                var pdfOptions = new PdfGenerationOptions
                {
                    OrganizationName = "National Telecommunications Commission",
                    IncludePageNumbers = true,
                    IncludeTimestamp = true
                };
                fileBytes = _markdownToPdfService.ConvertToPdf(cleanedContent, response.Data.Title ?? "AI Generated Report", pdfOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert markdown to PDF for report {ReportId}", id);
                return BadRequest(new { message = "Failed to generate PDF. Please try downloading as markdown instead." });
            }
        }
        else
        {
            fileBytes = System.Text.Encoding.UTF8.GetBytes(response.Data.Content);
        }

        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// Delete a generated report
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Successfully deleted report</response>
    /// <response code="404">Report not found</response>
    /// <response code="401">Unauthorized - authentication required</response>
    [HttpDelete("reports/{id}")]
    [RequirePermission(Permissions.GenerateReports)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteReport(Guid id)
    {
        var command = new DeleteReportCommand(id);
        var response = await _mediator.Send(command);
        return response.Succeeded ? Ok(response) : NotFound(response);
    }

    /// <summary>
    /// Strips outer markdown code fence wrapper (```markdown ... ```)
    /// </summary>
    /// <remarks>
    /// This only removes the OUTERMOST wrapper fence. Any code blocks
    /// (e.g., ```python, ```json) nested inside the content will be preserved
    /// and rendered correctly as code blocks by the Markdown component.
    /// </remarks>
    private static string StripOuterMarkdownFence(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var trimmed = content.Trim();
        
        // Check if content starts with ```markdown or ``` and ends with ```
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                var lastFence = trimmed.LastIndexOf("```");
                if (lastFence > firstNewline)
                {
                    // Extract content between first line and last ```
                    return trimmed.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                }
            }
        }

        return content;
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
