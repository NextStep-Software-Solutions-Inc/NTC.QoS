using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Handlers;

/// <summary>
/// Handler for starting a wireless broadband report background job
/// </summary>
public class StartWirelessBroadbandReportJobHandler : IRequestHandler<StartWirelessBroadbandReportJobCommand, string>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<StartWirelessBroadbandReportJobHandler> _logger;

    public StartWirelessBroadbandReportJobHandler(
        IBackgroundJobClient backgroundJobClient,
        IUserContextService userContextService,
        ILogger<StartWirelessBroadbandReportJobHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _userContextService = userContextService;
        _logger = logger;
    }

    public Task<string> Handle(StartWirelessBroadbandReportJobCommand request, CancellationToken cancellationToken)
    {
        // Generate unique job ID
        var jobId = Guid.NewGuid().ToString();
        var userId = _userContextService.GetCurrentUserId() ?? "system";

        // Create job request DTO
        var jobRequest = new WirelessBroadbandReportJobRequestDto
        {
            JobId = jobId,
            UserId = userId,
            RegionId = request.RegionId,
            ProvinceId = request.ProvinceId,
            MunicipalityId = request.MunicipalityId,
            FilterByStatus = request.FilterByStatus,
            Format = request.Format,
            IncludeScores = request.IncludeScores
        };

        // Enqueue background job
        _backgroundJobClient.Enqueue<IProcessWirelessBroadbandReportJob>(
            job => job.ExecuteAsync(jobRequest, CancellationToken.None));

        _logger.LogInformation(
            "Wireless broadband report job {JobId} queued for user {UserId}. Region: {RegionId}, Province: {ProvinceId}, Municipality: {MunicipalityId}",
            jobId, userId, request.RegionId, request.ProvinceId, request.MunicipalityId);

        return Task.FromResult(jobId);
    }
}
