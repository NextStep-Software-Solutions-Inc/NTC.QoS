using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Commands;

/// <summary>
/// Handler for AI report generation requests with RAG context and streaming response.
/// 
/// <para><b>COMPLETE FLOW:</b></para>
/// <list type="number">
///   <item>
///     <description><b>Load Global AI Instruction:</b> Retrieves the base system instruction from 
///     "Global AI Instruction.md" via <see cref="IAIInstructionService"/>. This defines the AI's 
///     scope, limitations, and project context for the CMTS &amp; Broadband Mapping Project.</description>
///   </item>
///   <item>
///     <description><b>RAG Context Retrieval:</b> Generates an embedding vector for the user's 
///     prompt using <see cref="IGenAIService.GetEmbeddingAsync"/>, then searches for the top 3 
///     most semantically similar previously generated reports using 
///     <see cref="IGeneratedReportRepository.FindSimilarReportsAsync"/>.</description>
///   </item>
///   <item>
///     <description><b>Build Enhanced Prompt:</b> Constructs the final prompt with:
///     <list type="bullet">
///       <item>SYSTEM INSTRUCTION: Global AI instruction (scope, limitations, project context)</item>
///       <item>CONTEXT FROM PREVIOUS REPORTS: RAG-retrieved report contents</item>
///       <item>USER REQUEST: Current user prompt</item>
///     </list>
///     </description>
///   </item>
///   <item>
///     <description><b>Stream AI Response:</b> Calls <see cref="IGenAIService.GenerateReportStreamAsync"/> 
///     which pipes the Google GenAI response directly to the client via Server-Sent Events (SSE). 
///     Each chunk is JSON-serialized and flushed immediately for real-time streaming.</description>
///   </item>
///   <item>
///     <description><b>Save Generated Report:</b> After streaming completes:
///     <list type="bullet">
///       <item>Extracts title from first markdown heading (# Title) or uses truncated prompt</item>
///       <item>Generates embedding for the report content for future RAG retrieval</item>
///       <item>Saves report with embedding to database via <see cref="IGeneratedReportRepository.AddReportWithEmbeddingAsync"/></item>
///     </list>
///     </description>
///   </item>
/// </list>
/// 
/// <para><b>EMBEDDING STORAGE:</b></para>
/// <para>Reports are stored with 768-dimensional embeddings (from text-embedding-004 model) in 
/// PostgreSQL using the pgvector extension. An HNSW index enables fast similarity search for 
/// RAG context retrieval.</para>
/// </summary>
public class GenerateReportHandler : IRequestHandler<GenerateReportRequest, Unit>
{
    private readonly IGenAIService _genAIService;
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IAIInstructionService _aiInstructionService;
    private readonly IMCPServerService _mcpServerService;
    private readonly ILogger<GenerateReportHandler> _logger;
    
    private const int MaxRelevantReports = 3;

    public GenerateReportHandler(
        IGenAIService genAIService,
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        IAIInstructionService aiInstructionService,
        IMCPServerService mcpServerService,
        ILogger<GenerateReportHandler> logger)
    {
        _genAIService = genAIService;
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
        _aiInstructionService = aiInstructionService;
        _mcpServerService = mcpServerService;
        _logger = logger;
    }

    public async Task<Unit> Handle(GenerateReportRequest request, CancellationToken cancellationToken)
    {
        var reportContent = new StringBuilder();
        
        try
        {
            _logger.LogInformation("Processing streaming report generation request with {FileCount} files", request.Request.Files?.Length ?? 0);

            // RAG: Retrieve relevant context from previously generated reports
            string contextData = string.Empty;
            try
            {
                _logger.LogInformation("Generating embedding for RAG context retrieval");
                var queryVector = await _genAIService.GetEmbeddingAsync(request.Request.Prompt);
                
                var relevantReports = await _reportRepository.FindSimilarReportsAsync(
                    queryVector, 
                    MaxRelevantReports, 
                    cancellationToken);

                if (relevantReports.Count > 0)
                {
                    contextData = string.Join("\n\n---\n\n", relevantReports);
                    _logger.LogInformation("Found {Count} relevant reports for context", relevantReports.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve RAG context. Proceeding without context augmentation.");
            }

            // Get global AI instruction
            var globalInstruction = _aiInstructionService.GetGlobalInstruction();

            // Get MCP tools information
            var mcpTools = await _mcpServerService.GetToolsAsync();

            // Build enhanced prompt with global instruction, MCP tools, and RAG context
            var enhancedPrompt = BuildEnhancedPrompt(globalInstruction, request.Request.Prompt, contextData, mcpTools);

            await foreach (var chunk in _genAIService.GenerateReportStreamAsync(
                enhancedPrompt,
                request.Request.Files,
                cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                reportContent.Append(chunk);
                var jsonChunk = JsonSerializer.Serialize(chunk);
                await request.HttpResponse.WriteAsync($"data: {jsonChunk}\n\n", cancellationToken);
                await request.HttpResponse.Body.FlushAsync(cancellationToken);
            }

            _logger.LogInformation("Report streaming completed successfully");

            await SaveGeneratedReportAsync(request, reportContent.ToString(), cancellationToken);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming report generation");
            await request.HttpResponse.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n", cancellationToken);
            throw;
        }
    }

    private static string BuildEnhancedPrompt(string globalInstruction, string userPrompt, string contextData, string mcpTools)
    {
        var promptBuilder = new StringBuilder();
        
        // Add global AI instruction as system context (first priority)
        if (!string.IsNullOrEmpty(globalInstruction))
        {
            promptBuilder.AppendLine("SYSTEM INSTRUCTION:");
            promptBuilder.AppendLine(globalInstruction);
            promptBuilder.AppendLine();
        }
        
        // Add MCP tools information
        if (!string.IsNullOrEmpty(mcpTools))
        {
            promptBuilder.AppendLine("AVAILABLE DATABASE QUERY TOOLS:");
            promptBuilder.AppendLine("You have access to the following tools that allow you to directly query the NTC QoS database for real-time data.");
            promptBuilder.AppendLine("Instead of relying solely on attached files, you can use these tools to retrieve current data from the database.");
            promptBuilder.AppendLine("To use a tool, include a JSON code block in your analysis with the format:");
            promptBuilder.AppendLine("```mcp-tool-call");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("  \"tool\": \"tool_name\",");
            promptBuilder.AppendLine("  \"parameters\": { ... }");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Available tools:");
            promptBuilder.AppendLine(mcpTools);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("IMPORTANT: When you need database information, use these tools to get accurate, up-to-date data.");
            promptBuilder.AppendLine();
        }
        
        // Add RAG context if available
        if (!string.IsNullOrEmpty(contextData))
        {
            promptBuilder.AppendLine("CONTEXT FROM PREVIOUS REPORTS:");
            promptBuilder.AppendLine(contextData);
            promptBuilder.AppendLine();
        }
        
        // Add user's request
        promptBuilder.AppendLine("USER REQUEST:");
        promptBuilder.AppendLine(userPrompt);

        return promptBuilder.ToString();
    }

    private async Task SaveGeneratedReportAsync(GenerateReportRequest request, string content, CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(authUserId))
            {
                _logger.LogWarning("Cannot save report: User context not available");
                return;
            }

            // Look up the User entity to get the GUID ID
            var user = await _userRepository.FirstOrDefaultAsync(
                _userRepository.Query(u => u.AuthUserId == authUserId),
                cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Cannot save report: User not found for AuthUserId: {AuthUserId}", authUserId);
                return;
            }

            // Extract title from first markdown heading or use truncated prompt
            var title = ExtractTitleFromContent(content) ?? 
                        (request.Request.Prompt.Length > 100 
                            ? request.Request.Prompt.Substring(0, 100) + "..." 
                            : request.Request.Prompt);

            // Prepare file metadata
            string? fileMetadata = null;
            if (request.Request.Files != null && request.Request.Files.Length > 0)
            {
                var metadata = request.Request.Files.Select(f => new
                {
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    Size = f.Length
                }).ToList();
                fileMetadata = JsonSerializer.Serialize(metadata);
            }

            // Generate embedding for the report content (for RAG semantic search)
            float[]? embedding = null;
            try
            {
                _logger.LogInformation("Generating embedding for report content");
                embedding = await _genAIService.GetEmbeddingAsync(content);
                _logger.LogInformation("Successfully generated embedding with {Dimensions} dimensions", embedding.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for report. Report will be saved without embedding.");
            }

            var report = new GeneratedReport
            {
                Prompt = request.Request.Prompt,
                Content = content,
                FilesAnalyzed = request.Request.Files?.Length ?? 0,
                FileMetadata = fileMetadata,
                UserId = user.Id,
                GeneratedAt = DateTime.UtcNow,
                Title = title
            };

            // Use repository method to save with embedding (handles Pgvector in Infrastructure layer)
            await _reportRepository.AddReportWithEmbeddingAsync(report, embedding, cancellationToken);

            _logger.LogInformation("Generated report saved with ID: {ReportId}", report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save generated report to database");
            // Don't throw - report was already streamed successfully
        }
    }

    private static string? ExtractTitleFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Look for first markdown heading (# Title)
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("# "))
            {
                return trimmedLine.Substring(2).Trim();
            }
        }

        return null;
    }
}
