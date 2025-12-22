using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GenAI;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Commands;

/// <summary>
/// Handler for RAG-based chat requests with semantic context retrieval from generated reports
/// </summary>
public class RagChatHandler : IRequestHandler<RagChatRequest, Unit>
{
    private readonly IGenAIService _genAIService;
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RagChatHandler> _logger;
    
    private const int MaxRelevantReports = 3;
    private static readonly TimeSpan SessionExpiration = TimeSpan.FromMinutes(30);

    public RagChatHandler(
        IGenAIService genAIService,
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        IMemoryCache memoryCache,
        ILogger<RagChatHandler> logger)
    {
        _genAIService = genAIService;
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<Unit> Handle(RagChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing RAG chat request");

            // Get current user
            var authUserId = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(authUserId))
            {
                await WriteErrorResponse(request.HttpResponse, "User context not available", cancellationToken);
                return Unit.Value;
            }

            var user = await _userRepository.FirstOrDefaultAsync(
                _userRepository.Query(u => u.AuthUserId == authUserId),
                cancellationToken);

            if (user == null)
            {
                await WriteErrorResponse(request.HttpResponse, "User not found", cancellationToken);
                return Unit.Value;
            }

            // Session management using user ID as cache key
            var cacheKey = user.Id.ToString();
            var chatHistory = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = SessionExpiration;
                return new List<ChatMessage>();
            }) ?? [];

            // RAG: Embed the user's question and search for relevant reports
            var userMessage = request.Request.Message;
            string contextData = string.Empty;

            try
            {
                _logger.LogInformation("Generating embedding for user query");
                var queryVector = await _genAIService.GetEmbeddingAsync(userMessage);
                
                // Search for relevant reports using L2 distance (Euclidean)
                var relevantReports = await _reportRepository.Query(r => r.Embedding != null)
                    .OrderBy(r => r.Embedding!.L2Distance(new Vector(queryVector)))
                    .Take(MaxRelevantReports)
                    .Select(r => r.Content)
                    .ToListAsync(cancellationToken);

                if (relevantReports.Count > 0)
                {
                    contextData = string.Join("\n\n---\n\n", relevantReports);
                    _logger.LogInformation("Found {Count} relevant reports for context", relevantReports.Count);
                }
                else
                {
                    _logger.LogInformation("No relevant reports found for context");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve context from reports. Proceeding without RAG context.");
            }

            // Build chat history string
            var historyBuilder = new StringBuilder();
            foreach (var msg in chatHistory.TakeLast(10)) // Limit history to last 10 messages
            {
                historyBuilder.AppendLine($"{msg.Role.ToUpperInvariant()}: {msg.Content}");
            }
            var history = historyBuilder.ToString();

            // Build the enhanced prompt with context
            var promptBuilder = new StringBuilder();
            
            if (!string.IsNullOrEmpty(contextData))
            {
                promptBuilder.AppendLine("CONTEXT:");
                promptBuilder.AppendLine(contextData);
                promptBuilder.AppendLine();
            }
            
            if (!string.IsNullOrEmpty(history))
            {
                promptBuilder.AppendLine("CHAT HISTORY:");
                promptBuilder.AppendLine(history);
                promptBuilder.AppendLine();
            }
            
            promptBuilder.AppendLine("USER:");
            promptBuilder.AppendLine(userMessage);

            var enhancedPrompt = promptBuilder.ToString();
            _logger.LogInformation("Generated enhanced prompt with context length: {Length}", enhancedPrompt.Length);

            // Generate response
            var responseContent = new StringBuilder();
            await foreach (var chunk in StreamGenerateContentAsync(enhancedPrompt, request.HttpResponse, cancellationToken))
            {
                responseContent.Append(chunk);
            }

            // Update session history
            chatHistory.Add(new ChatMessage { Role = "user", Content = userMessage });
            chatHistory.Add(new ChatMessage { Role = "assistant", Content = responseContent.ToString() });
            
            // Save updated history to cache
            _memoryCache.Set(cacheKey, chatHistory, new MemoryCacheEntryOptions
            {
                SlidingExpiration = SessionExpiration
            });

            _logger.LogInformation("RAG chat completed successfully");
            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RAG chat request");
            await WriteErrorResponse(request.HttpResponse, ex.Message, cancellationToken);
            throw;
        }
    }

    private async IAsyncEnumerable<string> StreamGenerateContentAsync(
        string prompt, 
        HttpResponse response, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fullResponse = await _genAIService.GenerateContentAsync(prompt);
        
        // For non-streaming response, send as a single chunk
        var jsonChunk = JsonSerializer.Serialize(fullResponse);
        await response.WriteAsync($"data: {jsonChunk}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
        
        yield return fullResponse;
    }

    private static async Task WriteErrorResponse(HttpResponse response, string message, CancellationToken cancellationToken)
    {
        response.StatusCode = 400;
        await response.WriteAsync($"data: {{\"error\": \"{message}\"}}\n\n", cancellationToken);
    }
}
