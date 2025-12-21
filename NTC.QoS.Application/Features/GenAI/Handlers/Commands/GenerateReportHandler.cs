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
/// Handler for AI report generation requests with streaming response and persistence
/// </summary>
public class GenerateReportHandler : IRequestHandler<GenerateReportRequest, Unit>
{
    private readonly IGenAIService _genAIService;
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<GenerateReportHandler> _logger;

    public GenerateReportHandler(
        IGenAIService genAIService,
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        ILogger<GenerateReportHandler> logger)
    {
        _genAIService = genAIService;
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<Unit> Handle(GenerateReportRequest request, CancellationToken cancellationToken)
    {
        var reportContent = new StringBuilder();
        
        try
        {
            _logger.LogInformation("Processing streaming report generation request with {FileCount} files", request.Request.Files?.Length ?? 0);

            await foreach (var chunk in _genAIService.GenerateReportStreamAsync(
                request.Request.Prompt,
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

    private async Task SaveGeneratedReportAsync(GenerateReportRequest request, string content, CancellationToken cancellationToken)
    {
        try
        {
            var userIdString = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Cannot save report: User context not available");
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

            var report = new GeneratedReport
            {
                Prompt = request.Request.Prompt,
                Content = content,
                FilesAnalyzed = request.Request.Files?.Length ?? 0,
                FileMetadata = fileMetadata,
                UserId = userId,
                GeneratedAt = DateTime.UtcNow,
                Title = title
            };

            await _reportRepository.AddAsync(report, cancellationToken);
            await _reportRepository.SaveChangesAsync(cancellationToken);

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
