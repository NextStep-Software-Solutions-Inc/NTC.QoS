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
            var lastFlushTime = DateTime.UtcNow;
            const int debounceMs = 100; // Wait 100ms to accumulate rapid chunks
            const int minBufferSize = 30; // Minimum chars before considering flush
            
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
                var timeSinceLastFlush = (DateTime.UtcNow - lastFlushTime).TotalMilliseconds;

                // Flush if: (time elapsed AND buffer has content) OR (buffer is large enough with good boundary)
                bool shouldFlush = false;
                string? textToSend = null;

                if (timeSinceLastFlush >= debounceMs && buffer.Length > 0)
                {
                    // Time-based flush: send up to last good boundary
                    var lastNewline = bufferText.LastIndexOf('\n');
                    var lastSpace = bufferText.LastIndexOf(' ');
                    var breakPoint = Math.Max(lastNewline, lastSpace);

                    if (breakPoint > 0)
                    {
                        textToSend = bufferText.Substring(0, breakPoint + 1);
                        shouldFlush = true;
                    }
                    else if (buffer.Length >= minBufferSize * 2)
                    {
                        // Force flush if buffer is getting too large
                        textToSend = bufferText;
                        shouldFlush = true;
                    }
                }
                else if (buffer.Length >= minBufferSize && bufferText.Contains('\n'))
                {
                    // Size-based flush at newline boundary
                    var lastNewline = bufferText.LastIndexOf('\n');
                    textToSend = bufferText.Substring(0, lastNewline + 1);
                    shouldFlush = true;
                }

                if (shouldFlush && !string.IsNullOrEmpty(textToSend))
                {
                    await request.HttpResponse.WriteAsync($"data: {textToSend}\n\n", cancellationToken);
                    await request.HttpResponse.Body.FlushAsync(cancellationToken);
                    
                    buffer.Clear();
                    buffer.Append(bufferText.Substring(textToSend.Length));
                    lastFlushTime = DateTime.UtcNow;
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
