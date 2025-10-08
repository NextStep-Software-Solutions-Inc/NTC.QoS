using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

/// <summary>
/// Handler for starting background connectivity assessment jobs
/// Returns immediately with job ID for tracking, actual processing happens in background
/// </summary>
public class StartConnectivityAssessmentJobHandler : IRequestHandler<StartConnectivityAssessmentJobRequest, ApiResponse<ConnectivityAssessmentJobResponseDto>>
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<StartConnectivityAssessmentJobHandler> _logger;

    public StartConnectivityAssessmentJobHandler(
        IBackgroundJobService backgroundJobService,
        ILogger<StartConnectivityAssessmentJobHandler> logger)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    public Task<ApiResponse<ConnectivityAssessmentJobResponseDto>> Handle(
        StartConnectivityAssessmentJobRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var jobId = Guid.NewGuid().ToString();
            
            var jobRequest = new ConnectivityAssessmentJobRequest
            {
                JobId = jobId,
                LocationIds = request.LocationIds,
                LocationType = request.LocationType,
                ForceRecalculate = request.ForceRecalculate,
                GenerateHierarchicalAssessments = request.GenerateHierarchicalAssessments,
                HierarchicalDepth = request.HierarchicalDepth,
                UserId = request.UserId ?? "System",
                RequestedAt = DateTime.UtcNow
            };

            // Enqueue the background job
            var hangfireJobId = _backgroundJobService.Enqueue<IProcessConnectivityAssessmentJob>(
                job => job.ExecuteAsync(jobRequest, CancellationToken.None));

            _logger.LogInformation("Started background connectivity assessment job {JobId} (Hangfire: {HangfireJobId}) for {LocationCount} {LocationType}s. " +
                "Hierarchical: {Hierarchical}, Force: {Force}",
                jobId, hangfireJobId, request.LocationIds.Count, request.LocationType, 
                request.GenerateHierarchicalAssessments, request.ForceRecalculate);

            var response = new ConnectivityAssessmentJobResponseDto
            {
                JobId = jobId,
                HangfireJobId = hangfireJobId,
                Status = ConnectivityAssessmentJobStatusEnum.Pending,
                Message = "Connectivity assessment job has been queued for processing",
                EstimatedDurationMinutes = EstimateProcessingTime(request),
                RequestedAt = DateTime.UtcNow,
                TotalLocations = CalculateTotalLocations(request)
            };

            return Task.FromResult(new ApiResponse<ConnectivityAssessmentJobResponseDto>
            {
                Succeeded = true,
                Message = $"Background job started successfully. Job ID: {jobId}",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting background connectivity assessment job");
            return Task.FromResult(new ApiResponse<ConnectivityAssessmentJobResponseDto>
            {
                Succeeded = false,
                Message = "Failed to start background job",
                Errors = new Dictionary<string, string[]>
                {
                    { "JobError", [ex.Message] }
                }
            });
        }
    }

    /// <summary>
    /// Estimate processing time based on request parameters
    /// </summary>
    private static int EstimateProcessingTime(StartConnectivityAssessmentJobRequest request)
    {
        // Base time per location (in minutes)
        var baseTimePerLocation = request.ForceRecalculate ? 0.5 : 0.2;
        
        // Hierarchical multiplier
        var hierarchicalMultiplier = request.GenerateHierarchicalAssessments 
            ? request.LocationType switch
            {
                LocationTypeEnum.Regional => 50, // Regions have many children
                LocationTypeEnum.Provincial => 20, // Provinces have many municipalities
                LocationTypeEnum.Municipality => 10, // Municipalities have many barangays
                _ => 1
            }
            : 1;

        var estimatedMinutes = (int)Math.Ceiling(request.LocationIds.Count * baseTimePerLocation * hierarchicalMultiplier);
        
        // Minimum 1 minute, maximum 120 minutes for display purposes
        return Math.Max(1, Math.Min(120, estimatedMinutes));
    }

    /// <summary>
    /// Calculate total number of locations that will be processed
    /// </summary>
    private static int CalculateTotalLocations(StartConnectivityAssessmentJobRequest request)
    {
        if (!request.GenerateHierarchicalAssessments)
        {
            return request.LocationIds.Count;
        }

        // Estimate based on location type and hierarchy
        var estimatedChildrenPerLocation = request.LocationType switch
        {
            LocationTypeEnum.Regional when request.HierarchicalDepth >= 3 => 1000, // Region -> Provinces -> Municipalities -> Barangays
            LocationTypeEnum.Regional when request.HierarchicalDepth >= 2 => 100,  // Region -> Provinces -> Municipalities
            LocationTypeEnum.Regional => 20,                                       // Region -> Provinces only
            LocationTypeEnum.Provincial when request.HierarchicalDepth >= 2 => 500, // Province -> Municipalities -> Barangays
            LocationTypeEnum.Provincial => 50,                                     // Province -> Municipalities only
            LocationTypeEnum.Municipality => 30,                                   // Municipality -> Barangays
            _ => 0
        };

        return request.LocationIds.Count + (request.LocationIds.Count * estimatedChildrenPerLocation);
    }
}

/// <summary>
/// Response DTO for connectivity assessment job
/// </summary>
public class ConnectivityAssessmentJobResponseDto
{
    public string JobId { get; set; } = string.Empty;
    public string HangfireJobId { get; set; } = string.Empty;
    public ConnectivityAssessmentJobStatusEnum Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public DateTime RequestedAt { get; set; }
    public int TotalLocations { get; set; }
    
    /// <summary>
    /// URL to check job status
    /// </summary>
    public string StatusUrl => $"/api/reports/connectivity-assessment/job/{JobId}/status";
    
    /// <summary>
    /// URL to view Hangfire dashboard for this job
    /// </summary>
    public string HangfireUrl => $"/hangfire/jobs/details/{HangfireJobId}";
}