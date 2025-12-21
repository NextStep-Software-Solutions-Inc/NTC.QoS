using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.GenAI.Handlers.Commands;

/// <summary>
/// Handler for deleting a generated report
/// </summary>
public class DeleteReportCommandHandler : IRequestHandler<DeleteReportCommand, ApiResponse<bool>>
{
    private readonly IGeneratedReportRepository _reportRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<DeleteReportCommandHandler> _logger;

    public DeleteReportCommandHandler(
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        ILogger<DeleteReportCommandHandler> logger)
    {
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteReportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdString = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return new ApiResponse<bool>
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
                return new ApiResponse<bool>
                {
                    Succeeded = false,
                    Message = "Report not found or you don't have permission to delete it"
                };
            }

            _reportRepository.Delete(report);
            await _reportRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted report {ReportId} for user {UserId}", request.ReportId, userId);

            return new ApiResponse<bool>
            {
                Data = true,
                Succeeded = true,
                Message = "Report deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {ReportId}", request.ReportId);
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Failed to delete report"
            };
        }
    }
}
