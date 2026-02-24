using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GenAI;
using System.Text;
using System.Text.Json;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Commands;

/// <summary>
/// Handler for AI chat requests with RAG (Retrieval Augmented Generation) and streaming response.
/// 
/// <para><b>COMPLETE FLOW:</b></para>
/// <list type="number">
///   <item>
///     <description><b>Load Global AI Instruction:</b> Retrieves the base system instruction from 
///     "Global AI Instruction.md" via <see cref="IAIInstructionService"/>. This defines the AI's 
///     scope, limitations, and project context for the CMTS &amp; Broadband Mapping Project.</description>
///   </item>
///   <item>
///     <description><b>Session Management:</b> For authenticated users, retrieves or creates a 
///     chat history from <see cref="IMemoryCache"/> keyed by user ID. Sessions expire after 30 
///     minutes of inactivity.</description>
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
///       <item>CHAT HISTORY: Previous conversation messages (last 10)</item>
///       <item>USER: Current user prompt</item>
///     </list>
///     </description>
///   </item>
///   <item>
///     <description><b>Stream AI Response:</b> Calls <see cref="IGenAIService.ChatStreamAsync"/> 
///     which pipes the Google GenAI response directly to the client via Server-Sent Events (SSE). 
///     Each chunk is JSON-serialized and flushed immediately for real-time streaming.</description>
///   </item>
///   <item>
///     <description><b>Update Session History:</b> Adds the user message and AI response to the 
///     session cache for context in future requests.</description>
///   </item>
/// </list>
/// </summary>
public class ChatHandler : IRequestHandler<ChatRequest, Unit>
{
    private readonly IGenAIService _genAIService;
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IAIInstructionService _aiInstructionService;
    private readonly IMCPServerService _mcpServerService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ChatHandler> _logger;
    
    private const int MaxRelevantReports = 3;
    private static readonly TimeSpan SessionExpiration = TimeSpan.FromMinutes(30);

    public ChatHandler(
        IGenAIService genAIService,
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        IAIInstructionService aiInstructionService,
        IMCPServerService mcpServerService,
        IMemoryCache memoryCache,
        ILogger<ChatHandler> logger)
    {
        _genAIService = genAIService;
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
        _aiInstructionService = aiInstructionService;
        _mcpServerService = mcpServerService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<Unit> Handle(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing streaming chat request with {FileCount} files", request.Request.Files?.Length ?? 0);

            // Get current user for session management
            var authUserId = _userContextService.GetCurrentUserId();
            List<ChatMessage>? chatHistory = null;
            string? cacheKey = null;

            if (!string.IsNullOrEmpty(authUserId))
            {
                var user = await _userRepository.FirstOrDefaultAsync(
                    _userRepository.Query(u => u.AuthUserId == authUserId),
                    cancellationToken);

                if (user != null)
                {
                    cacheKey = user.Id.ToString();
                    chatHistory = _memoryCache.GetOrCreate(cacheKey, entry =>
                    {
                        entry.SlidingExpiration = SessionExpiration;
                        return new List<ChatMessage>();
                    });
                }
            }

            // RAG: Retrieve relevant context from generated reports
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

            // Build enhanced prompt with global instruction, MCP tools, RAG context and chat history
            var enhancedPrompt = BuildEnhancedPrompt(globalInstruction, request.Request.Prompt, contextData, chatHistory, mcpTools);

            // Stream response with file attachments support
            var responseContent = new StringBuilder();
            await foreach (var chunk in _genAIService.ChatStreamAsync(
                enhancedPrompt,
                request.Request.Files,
                cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                responseContent.Append(chunk);
                var jsonChunk = JsonSerializer.Serialize(chunk);
                await request.HttpResponse.WriteAsync($"data: {jsonChunk}\n\n", cancellationToken);
                await request.HttpResponse.Body.FlushAsync(cancellationToken);
            }

            // Update session history if user is authenticated
            if (chatHistory != null && cacheKey != null)
            {
                chatHistory.Add(new ChatMessage { Role = "user", Content = request.Request.Prompt });
                chatHistory.Add(new ChatMessage { Role = "assistant", Content = responseContent.ToString() });
                
                _memoryCache.Set(cacheKey, chatHistory, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = SessionExpiration
                });
            }

            _logger.LogInformation("Chat streaming completed successfully");
            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming chat request");
            await request.HttpResponse.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n", cancellationToken);
            throw;
        }
    }

    private static string BuildEnhancedPrompt(string globalInstruction, string userPrompt, string contextData, List<ChatMessage>? chatHistory, string mcpTools)
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
            promptBuilder.AppendLine("To use a tool, include a JSON code block in your response with the format:");
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
        
        // Add chat history if available
        if (chatHistory != null && chatHistory.Count > 0)
        {
            promptBuilder.AppendLine("CHAT HISTORY:");
            foreach (var msg in chatHistory.TakeLast(10))
            {
                promptBuilder.AppendLine($"{msg.Role.ToUpperInvariant()}: {msg.Content}");
            }
            promptBuilder.AppendLine();
        }
        
        // Add user's current prompt
        promptBuilder.AppendLine("USER:");
        promptBuilder.AppendLine(userPrompt);

        return promptBuilder.ToString();
    }
}
