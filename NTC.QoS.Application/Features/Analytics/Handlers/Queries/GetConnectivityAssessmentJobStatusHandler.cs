using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

/// <summary>
/// Enhanced handler for getting connectivity assessment job status
/// Includes database verification, stale status detection, and health checks
/// </summary>
public class GetConnectivityAssessmentJobStatusHandler : IRequestHandler<GetConnectivityAssessmentJobStatusRequest, ApiResponse<ConnectivityAssessmentJobStatusDto>>
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<GetConnectivityAssessmentJobStatusHandler> _logger;
    private readonly IConnectivityAssessmentRepository _connectivityRepository;

    public GetConnectivityAssessmentJobStatusHandler(
        IMemoryCache memoryCache,
        ILogger<GetConnectivityAssessmentJobStatusHandler> logger,
        IConnectivityAssessmentRepository connectivityRepository)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _connectivityRepository = connectivityRepository;
    }

    public async Task<ApiResponse<ConnectivityAssessmentJobStatusDto>> Handle(
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
                return new ApiResponse<ConnectivityAssessmentJobStatusDto>
                {
                    Succeeded = false,
                    Message = $"Job {request.JobId} not found or has expired",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "JobNotFound", [$"No job found with ID {request.JobId}"] }
                    }
                };
            }

            // Check for stale status
            if (jobStatus.IsStale && jobStatus.Status == ConnectivityAssessmentJobStatusEnum.Processing)
            {
                _logger.LogWarning("Job {JobId} has stale status. Last updated: {LastUpdated}, Current time: {CurrentTime}",
                    request.JobId, jobStatus.LastUpdated, DateTime.UtcNow);
            }

            // For completed jobs, perform database verification if not already done or if requested explicitly
            if (jobStatus.Status == ConnectivityAssessmentJobStatusEnum.Completed && 
                (!jobStatus.DatabaseVerified || request.ForceVerification))
            {
                await PerformDatabaseVerification(jobStatus, cancellationToken);
            }

            _logger.LogDebug("Retrieved status for job {JobId}: {Status} - {Message}. Health: {Health}",
                request.JobId, jobStatus.Status, jobStatus.Message, jobStatus.HealthStatus);

            // Build response message with health information
            var responseMessage = BuildResponseMessage(jobStatus);

            return new ApiResponse<ConnectivityAssessmentJobStatusDto>
            {
                Succeeded = true,
                Message = responseMessage,
                Data = jobStatus
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", request.JobId);
            return new ApiResponse<ConnectivityAssessmentJobStatusDto>
            {
                Succeeded = false,
                Message = "Error retrieving job status",
                Errors = new Dictionary<string, string[]>
                {
                    { "StatusError", [ex.Message] }
                }
            };
        }
    }

    /// <summary>
    /// Performs database verification to ensure cached status matches reality
    /// </summary>
    private async Task PerformDatabaseVerification(
        ConnectivityAssessmentJobStatusDto jobStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get job request details from cache to know what locations to verify
            var jobRequestCacheKey = $"job_request_{jobStatus.JobId}";
            var jobRequest = _memoryCache.Get<ConnectivityAssessmentJobRequestDto>(jobRequestCacheKey);

            if (jobRequest == null)
            {
                _logger.LogWarning("Job {JobId}: Cannot perform database verification - job request not found in cache",
                    jobStatus.JobId);
                return;
            }

            _logger.LogDebug("Job {JobId}: Performing database verification for {LocationCount} {LocationType}s",
                jobStatus.JobId, jobRequest.LocationIds.Count, jobRequest.LocationType);

            // Query database to get actual saved assessments
            var dbAssessments = await _connectivityRepository.GetByLocationIdsAsync(
                jobRequest.LocationIds,
                jobRequest.LocationType,
                cancellationToken);

            var dbCount = dbAssessments?.Count ?? 0;

            // Update status with verification results
            jobStatus.DatabaseVerified = true;
            jobStatus.DatabaseVerifiedCount = dbCount;
            jobStatus.DatabaseVerificationTime = DateTime.UtcNow;

            // Sample first 5 location IDs for verification
            if (dbAssessments != null && dbAssessments.Count > 0)
            {
                jobStatus.SampleLocationIds = dbAssessments
                    .Take(5)
                    .Select(a => a.LocationId)
                    .ToList();
            }

            // Log results
            if (jobStatus.HasDatabaseDiscrepancy)
            {
                _logger.LogWarning(
                    "Job {JobId}: DATABASE DISCREPANCY DETECTED! Processed: {ProcessedCount}, Database: {DbCount}, Missing: {Missing}",
                    jobStatus.JobId, jobStatus.ProcessedCount, dbCount, jobStatus.MissingInDatabase);
            }
            else
            {
                _logger.LogInformation(
                    "Job {JobId}: Database verification successful. Confirmed {DbCount} assessments in database",
                    jobStatus.JobId, dbCount);
            }

            // Update the cache with verified status
            var statusCacheKey = $"job_status_{jobStatus.JobId}";
            _memoryCache.Set(statusCacheKey, jobStatus, TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Error performing database verification", jobStatus.JobId);
            jobStatus.DatabaseVerified = false;
        }
    }

    /// <summary>
    /// Builds a comprehensive response message based on job status and health
    /// </summary>
    private static string BuildResponseMessage(ConnectivityAssessmentJobStatusDto jobStatus)
    {
        var messages = new List<string> { "Job status retrieved successfully" };

        // Add stale warning
        if (jobStatus.IsStale)
        {
            messages.Add("WARNING: Job status is stale (not updated in last 5 minutes)");
        }

        // Add database discrepancy warning
        if (jobStatus.HasDatabaseDiscrepancy)
        {
            messages.Add($"WARNING: Database discrepancy detected - {jobStatus.MissingInDatabase} assessments missing from database");
        }

        // Add completion status
        if (jobStatus.Status == ConnectivityAssessmentJobStatusEnum.Completed)
        {
            if (jobStatus.DatabaseVerified)
            {
                messages.Add($"Database verified: {jobStatus.DatabaseVerifiedCount}/{jobStatus.ProcessedCount} assessments confirmed");
            }
            else
            {
                messages.Add("Database verification pending");
            }
        }

        // Add failed locations count
        if (jobStatus.FailedLocationCount.HasValue && jobStatus.FailedLocationCount.Value > 0)
        {
            messages.Add($"{jobStatus.FailedLocationCount} locations failed during processing");
        }

        return string.Join(". ", messages);
    }
}