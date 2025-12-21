using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Commands;

/// <summary>
/// Handler for AI chat requests with streaming response
/// </summary>
public class ChatHandler : IRequestHandler<ChatRequest, Unit>
{
    private readonly IGenAIService _genAIService;
    private readonly ILogger<ChatHandler> _logger;

    public ChatHandler(IGenAIService genAIService, ILogger<ChatHandler> logger)
    {
        _genAIService = genAIService;
        _logger = logger;
    }

    public async Task<Unit> Handle(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing streaming chat request with {FileCount} files", request.Request.Files?.Length ?? 0);

            var buffer = new System.Text.StringBuilder();
            
            await foreach (var chunk in _genAIService.ChatStreamAsync(
                request.Request.Prompt,
                request.Request.Files,
                cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                buffer.Append(chunk);
                var bufferText = buffer.ToString();

                // Send when we hit a newline (complete line/paragraph) to preserve markdown integrity
                if (bufferText.Contains('\n'))
                {
                    var lastNewlineIndex = bufferText.LastIndexOf('\n');
                    var textToSend = bufferText.Substring(0, lastNewlineIndex + 1);
                    
                    await request.HttpResponse.WriteAsync($"data: {textToSend}\n\n", cancellationToken);
                    await request.HttpResponse.Body.FlushAsync(cancellationToken);
                    
                    // Keep the remainder in buffer
                    buffer.Clear();
                    buffer.Append(bufferText.Substring(lastNewlineIndex + 1));
                }
            }

            // Send any remaining buffered content
            if (buffer.Length > 0)
            {
                await request.HttpResponse.WriteAsync($"data: {buffer}\n\n", cancellationToken);
                await request.HttpResponse.Body.FlushAsync(cancellationToken);
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
}
