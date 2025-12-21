using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Queries;

/// <summary>
/// Handler for retrieving user's generated reports
/// </summary>
public class GetUserReportsQueryHandler : IRequestHandler<GetUserReportsQuery, ApiResponse<List<GeneratedReportSummaryDto>>>
{
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<GetUserReportsQueryHandler> _logger;

    public GetUserReportsQueryHandler(
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        ILogger<GetUserReportsQueryHandler> logger)
    {
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<GeneratedReportSummaryDto>>> Handle(
        GetUserReportsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdString = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return new ApiResponse<List<GeneratedReportSummaryDto>>
                {
                    Succeeded = false,
                    Message = "User not authenticated"
                };
            }

            var reports = await _reportRepository.GetUserReportsAsync(
                userId,
                request.Skip,
                request.Take,
                cancellationToken);

            var reportDtos = reports.Select(r => new GeneratedReportSummaryDto
            {
                Id = r.Id,
                Title = r.Title,
                PromptPreview = r.Prompt.Length > 150 ? r.Prompt.Substring(0, 150) + "..." : r.Prompt,
                FilesAnalyzed = r.FilesAnalyzed,
                GeneratedAt = r.GeneratedAt,
                ContentPreview = r.Content.Length > 200 ? r.Content.Substring(0, 200) + "..." : r.Content
            }).ToList();

            _logger.LogInformation("Retrieved {Count} reports for user {UserId}", reportDtos.Count, userId);

            return new ApiResponse<List<GeneratedReportSummaryDto>>
            {
                Data = reportDtos,
                Succeeded = true,
                Message = $"Retrieved {reportDtos.Count} report(s)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user reports");
            return new ApiResponse<List<GeneratedReportSummaryDto>>
            {
                Succeeded = false,
                Message = "Failed to retrieve reports"
            };
        }
    }
}
