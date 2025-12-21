using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Queries;

/// <summary>
/// Handler for retrieving a specific generated report
/// </summary>
public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ApiResponse<GeneratedReportDto>>
{
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<GetReportByIdQueryHandler> _logger;

    public GetReportByIdQueryHandler(
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        ILogger<GetReportByIdQueryHandler> logger)
    {
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<ApiResponse<GeneratedReportDto>> Handle(
        GetReportByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdString = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return new ApiResponse<GeneratedReportDto>
                {
                    Succeeded = false,
                    Message = "User not authenticated"
                };
            }

            var report = await _reportRepository.GetUserReportByIdAsync(
                request.ReportId,
                userId,
                cancellationToken);

            if (report == null)
            {
                return new ApiResponse<GeneratedReportDto>
                {
                    Succeeded = false,
                    Message = "Report not found"
                };
            }

            var reportDto = new GeneratedReportDto
            {
                Id = report.Id,
                Title = report.Title,
                Prompt = report.Prompt,
                Content = report.Content,
                FilesAnalyzed = report.FilesAnalyzed,
                FileMetadata = report.FileMetadata,
                GeneratedAt = report.GeneratedAt,
                GeneratedBy = report.User != null 
                    ? $"{report.User.FirstName} {report.User.LastName}".Trim() 
                    : "Unknown"
            };

            _logger.LogInformation("Retrieved report {ReportId} for user {UserId}", request.ReportId, userId);

            return new ApiResponse<GeneratedReportDto>
            {
                Data = reportDto,
                Succeeded = true,
                Message = "Report retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report {ReportId}", request.ReportId);
            return new ApiResponse<GeneratedReportDto>
            {
                Succeeded = false,
                Message = "Failed to retrieve report"
            };
        }
    }
}
