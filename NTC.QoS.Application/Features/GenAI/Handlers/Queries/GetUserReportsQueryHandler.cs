using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Queries;

/// <summary>
/// Handler for retrieving user's generated reports with pagination
/// </summary>
public class GetUserReportsQueryHandler : IRequestHandler<GetUserReportsQuery, PaginatedResponse<GeneratedReportSummaryDto>>
{
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserReportsQueryHandler> _logger;

    public GetUserReportsQueryHandler(
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        ILogger<GetUserReportsQueryHandler> logger)
    {
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<GeneratedReportSummaryDto>> Handle(
        GetUserReportsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(authUserId))
            {
                return new PaginatedResponse<GeneratedReportSummaryDto>([], 0, 0, 0)
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
                return new PaginatedResponse<GeneratedReportSummaryDto>([], 0, 0, 0)
                {
                    Succeeded = false,
                    Message = "User not found"
                };
            }

            // Build query for user's reports with DTO projection
            var reportDtosQuery = _reportRepository.Query(r => r.UserId == user.Id)
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new GeneratedReportSummaryDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    PromptPreview = r.Prompt.Length > 150 ? r.Prompt.Substring(0, 150) + "..." : r.Prompt,
                    FilesAnalyzed = r.FilesAnalyzed,
                    GeneratedAt = r.GeneratedAt,
                    ContentPreview = r.Content.Length > 200 ? r.Content.Substring(0, 200) + "..." : r.Content
                });

            return await _reportRepository.PaginateAsync(reportDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user reports");
            return new PaginatedResponse<GeneratedReportSummaryDto>([], 0, 0, 0)
            {
                Succeeded = false,
                Message = "Failed to retrieve reports"
            };
        }
    }
}
