using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Commands;

/// <summary>
/// Handler for AI chat requests
/// </summary>
public class ChatHandler : IRequestHandler<ChatRequest, ApiResponse<ChatResponseDto>>
{
    private readonly IGenAIService _genAIService;
    private readonly ILogger<ChatHandler> _logger;

    public ChatHandler(IGenAIService genAIService, ILogger<ChatHandler> logger)
    {
        _genAIService = genAIService;
        _logger = logger;
    }

    public async Task<ApiResponse<ChatResponseDto>> Handle(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing chat request with {FileCount} files", request.Request.Files?.Count ?? 0);

            var response = await _genAIService.ChatAsync(
                request.Request.Prompt,
                request.Request.Files,
                cancellationToken);

            var chatResponse = new ChatResponseDto
            {
                Response = response,
                FilesProcessed = request.Request.Files?.Count ?? 0,
                Timestamp = DateTime.UtcNow
            };

            return new ApiResponse<ChatResponseDto>
            {
                Data = chatResponse,
                Succeeded = true,
                Message = "Chat response generated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return new ApiResponse<ChatResponseDto>
            {
                Succeeded = false,
                Message = $"Failed to process chat request: {ex.Message}"
            };
        }
    }
}
