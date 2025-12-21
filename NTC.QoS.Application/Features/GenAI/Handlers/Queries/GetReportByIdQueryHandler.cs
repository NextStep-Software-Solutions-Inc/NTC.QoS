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
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetReportByIdQueryHandler> _logger;

    public GetReportByIdQueryHandler(
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        ILogger<GetReportByIdQueryHandler> logger)
    {
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<GeneratedReportDto>> Handle(
        GetReportByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(authUserId))
            {
                return new ApiResponse<GeneratedReportDto>
                {
                    Succeeded = false,
                    Message = "User not authenticated"
                };
            }

            // Look up the User entity to get the GUID ID
            var user = await _userRepository.FirstOrDefaultAsync(
                _userRepository.Query(u => u.AuthUserId == authUserId),
                cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for AuthUserId: {AuthUserId}", authUserId);
                return new ApiResponse<GeneratedReportDto>
                {
                    Succeeded = false,
                    Message = "User not found"
                };
            }

            var report = await _reportRepository.GetUserReportByIdAsync(
                request.ReportId,
                user.Id,
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
