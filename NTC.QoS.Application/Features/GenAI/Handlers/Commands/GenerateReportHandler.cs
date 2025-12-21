using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Commands;

/// <summary>
/// Handler for AI report generation requests
/// </summary>
public class GenerateReportHandler : IRequestHandler<GenerateReportRequest, ApiResponse<GenerateReportResponseDto>>
{
    private readonly IGenAIService _genAIService;
    private readonly ILogger<GenerateReportHandler> _logger;

    public GenerateReportHandler(IGenAIService genAIService, ILogger<GenerateReportHandler> logger)
    {
        _genAIService = genAIService;
        _logger = logger;
    }

    public async Task<ApiResponse<GenerateReportResponseDto>> Handle(GenerateReportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing report generation request with {FileCount} files", request.Request.Files?.Count ?? 0);

            var reportContent = await _genAIService.GenerateReportAsync(
                request.Request.Prompt,
                request.Request.Files,
                cancellationToken);

            var reportResponse = new GenerateReportResponseDto
            {
                ReportContent = reportContent,
                FilesAnalyzed = request.Request.Files?.Count ?? 0,
                GeneratedAt = DateTime.UtcNow
            };

            return new ApiResponse<GenerateReportResponseDto>
            {
                Data = reportResponse,
                Succeeded = true,
                Message = "Report generated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return new ApiResponse<GenerateReportResponseDto>
            {
                Succeeded = false,
                Message = $"Failed to generate report: {ex.Message}"
            };
        }
    }
}
