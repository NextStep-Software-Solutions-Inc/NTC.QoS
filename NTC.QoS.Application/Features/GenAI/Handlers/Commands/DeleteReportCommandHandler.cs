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
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DeleteReportCommandHandler> _logger;

    public DeleteReportCommandHandler(
        IGeneratedReportRepository reportRepository,
        IUserContextService userContextService,
        IUserRepository userRepository,
        ILogger<DeleteReportCommandHandler> logger)
    {
        _reportRepository = reportRepository;
        _userContextService = userContextService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteReportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var authUserId = _userContextService.GetCurrentUserId();
            if (string.IsNullOrEmpty(authUserId))
            {
                return new ApiResponse<bool>
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
                return new ApiResponse<bool>
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
                return new ApiResponse<bool>
                {
                    Succeeded = false,
                    Message = "Report not found or you don't have permission to delete it"
                };
            }

            _reportRepository.Delete(report);
            await _reportRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted report {ReportId} for user {UserId}", request.ReportId, user.Id);

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
