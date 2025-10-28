using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

/// <summary>
/// Handler for getting connectivity assessment job status
/// </summary>
public class GetConnectivityAssessmentJobStatusHandler : IRequestHandler<GetConnectivityAssessmentJobStatusRequest, ApiResponse<ConnectivityAssessmentJobStatusDto>>
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<GetConnectivityAssessmentJobStatusHandler> _logger;

    public GetConnectivityAssessmentJobStatusHandler(
        IMemoryCache memoryCache,
        ILogger<GetConnectivityAssessmentJobStatusHandler> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<ApiResponse<ConnectivityAssessmentJobStatusDto>> Handle(
        GetConnectivityAssessmentJobStatusRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Get job status from cache
            var cacheKey = $"job_status_{request.JobId}";
            var jobStatus = _memoryCache.Get<ConnectivityAssessmentJobStatusDto>(cacheKey);
            
            if (jobStatus == null)
            {
                return Task.FromResult(new ApiResponse<ConnectivityAssessmentJobStatusDto>
                {
                    Succeeded = false,
                    Message = $"Job {request.JobId} not found or has expired",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "JobNotFound", [$"No job found with ID {request.JobId}"] }
                    }
                });
            }

            _logger.LogDebug("Retrieved status for job {JobId}: {Status} - {Message}", 
                request.JobId, jobStatus.Status, jobStatus.Message);

            return Task.FromResult(new ApiResponse<ConnectivityAssessmentJobStatusDto>
            {
                Succeeded = true,
                Message = "Job status retrieved successfully",
                Data = jobStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", request.JobId);
            return Task.FromResult(new ApiResponse<ConnectivityAssessmentJobStatusDto>
            {
                Succeeded = false,
                Message = "Error retrieving job status",
                Errors = new Dictionary<string, string[]>
                {
                    { "StatusError", [ex.Message] }
                }
            });
        }
    }
}