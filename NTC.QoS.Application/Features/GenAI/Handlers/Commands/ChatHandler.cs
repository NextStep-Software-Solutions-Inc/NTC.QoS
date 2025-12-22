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
/// Handler for AI chat requests with RAG (Retrieval Augmented Generation) and streaming response
/// </summary>
public class ChatHandler : IRequestHandler<ChatRequest, Unit>
{
    private readonly IGenAIService _genAIService;
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ChatHandler> _logger;
    
    private const int MaxRelevantReports = 3;
    private static readonly TimeSpan SessionExpiration = TimeSpan.FromMinutes(30);

    public ChatHandler(
        IGenAIService genAIService,
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        IMemoryCache memoryCache,
        ILogger<ChatHandler> logger)
    {
        _genAIService = genAIService;
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
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

            // Build enhanced prompt with RAG context and chat history
            var enhancedPrompt = BuildEnhancedPrompt(request.Request.Prompt, contextData, chatHistory);

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

    private static string BuildEnhancedPrompt(string userPrompt, string contextData, List<ChatMessage>? chatHistory)
    {
        var promptBuilder = new StringBuilder();
        
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
