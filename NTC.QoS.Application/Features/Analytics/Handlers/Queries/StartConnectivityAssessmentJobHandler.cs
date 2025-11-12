using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

/// <summary>
/// Handler for starting background connectivity assessment jobs
/// Returns immediately with job ID for tracking, actual processing happens in background
/// </summary>
public class StartConnectivityAssessmentJobHandler : IRequestHandler<StartConnectivityAssessmentJobRequest, ApiResponse<ConnectivityAssessmentJobResponseDto>>
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<StartConnectivityAssessmentJobHandler> _logger;
    private readonly IUserContextService _userContextService;
    private static int _jobIdCounter = 1000; // Start from 1000 for easier identification

    public StartConnectivityAssessmentJobHandler(
        IBackgroundJobService backgroundJobService,
        IMemoryCache memoryCache,
        ILogger<StartConnectivityAssessmentJobHandler> logger,
        IUserContextService userContextService)
    {
        _backgroundJobService = backgroundJobService;
        _memoryCache = memoryCache;
        _logger = logger;
        _userContextService = userContextService;
    }

    public Task<ApiResponse<ConnectivityAssessmentJobResponseDto>> Handle(
        StartConnectivityAssessmentJobRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate sequential integer JobId for easier querying
            var jobId = Interlocked.Increment(ref _jobIdCounter);
            var estimatedTotalLocations = CalculateEstimatedTotalLocations(request);
            var startTime = DateTime.UtcNow;
            
            // Get the current user ID from context (auto-captured from JWT token)
            var currentUserId = _userContextService.GetCurrentUserId() ?? "System";
            
            var jobRequest = new ConnectivityAssessmentJobRequestDto
            {
                JobId = jobId,
                LocationIds = request.LocationIds,
                LocationType = request.LocationType,
                ForceRecalculate = request.ForceRecalculate,
                IncludeBandBreakdown = request.IncludeBandBreakdown,
                GenerateHierarchicalAssessments = request.GenerateHierarchicalAssessments,
                HierarchicalDepth = request.HierarchicalDepth,
                
                // NEW: Set hierarchical processing configuration with safe defaults
                EnableHierarchicalProcessing = request.EnableHierarchicalProcessing ?? false, // Default disabled
                MaxHierarchicalLocations = request.MaxHierarchicalLocations ?? GetDefaultMaxHierarchicalLocations(request.LocationType),
                HierarchicalBatchSize = request.HierarchicalBatchSize ?? GetDefaultHierarchicalBatchSize(request.LocationType),
                
                // Date range filtering for speedtest and wireless validation data
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                
                UserId = currentUserId, // Use the auto-captured user ID from context
                RequestedAt = startTime
            };

            // Log the configuration for transparency
            _logger.LogInformation("Job {JobId} configuration - Hierarchical requested: {Requested}, Enabled: {Enabled}, " +
                "Max locations: {MaxLocations}, Batch size: {BatchSize}",
                jobId, request.GenerateHierarchicalAssessments, jobRequest.EnableHierarchicalProcessing,
                jobRequest.MaxHierarchicalLocations, jobRequest.HierarchicalBatchSize);

            // Calculate refined estimated total locations based on limits and configuration
            var refinedEstimatedLocations = CalculateRefinedEstimatedTotalLocations(jobRequest);

            // Log calculation breakdown for debugging
            if (jobRequest.GenerateHierarchicalAssessments && jobRequest.EnableHierarchicalProcessing)
            {
                var primaryCount = jobRequest.LocationIds.Count;
                var estimatedChildrenCount = refinedEstimatedLocations - primaryCount;
                _logger.LogInformation("Job {JobId} location estimation - Primary: {Primary}, Estimated children: {EstimatedChildren}, " +
                    "Total estimated: {TotalEstimated}, LocationType: {LocationType}, Depth: {Depth}",
                    jobId, primaryCount, estimatedChildrenCount, refinedEstimatedLocations, 
                    jobRequest.LocationType, jobRequest.HierarchicalDepth);
            }

            // Initialize job status in memory cache with all required fields
            var initialJobStatus = new ConnectivityAssessmentJobStatusDto
            {
                JobId = jobId,
                Status = ConnectivityAssessmentJobStatusEnum.Pending,
                Message = "Job is queued and waiting to start",
                LastUpdated = startTime,
                StartTime = null, // Will be set when job actually starts
                EndTime = null,
                ProcessedCount = 0,
                EstimatedTotalCount = refinedEstimatedLocations
            };

            var cacheKey = $"job_status_{jobId}";
            _memoryCache.Set(cacheKey, initialJobStatus, TimeSpan.FromHours(24));

            // Enqueue the background job
            var hangfireJobId = _backgroundJobService.Enqueue<IProcessConnectivityAssessmentJob>(
                job => job.ExecuteAsync(jobRequest, CancellationToken.None));

            _logger.LogInformation("Started background connectivity assessment job {JobId} (Hangfire: {HangfireJobId}) for {LocationCount} {LocationType}s. " +
                "Hierarchical requested: {Hierarchical}, Enabled: {Enabled}, Force: {Force}, EstimatedTotal: {EstimatedTotal}",
                jobId, hangfireJobId, request.LocationIds.Count, request.LocationType, 
                request.GenerateHierarchicalAssessments, jobRequest.EnableHierarchicalProcessing, 
                request.ForceRecalculate, refinedEstimatedLocations);

            var response = new ConnectivityAssessmentJobResponseDto
            {
                JobId = jobId,
                Status = ConnectivityAssessmentJobStatusEnum.Pending,
                Message = GetJobQueueMessage(jobRequest),
                EstimatedDurationMinutes = EstimateProcessingTime(jobRequest),
                EstimatedTotalLocations = refinedEstimatedLocations
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
    /// Get appropriate message based on hierarchical configuration
    /// </summary>
    private static string GetJobQueueMessage(ConnectivityAssessmentJobRequestDto jobRequest)
    {
        if (jobRequest.GenerateHierarchicalAssessments && jobRequest.EnableHierarchicalProcessing)
        {
            return $"Connectivity assessment job (with hierarchical processing) has been queued for processing. Max hierarchical locations: {jobRequest.MaxHierarchicalLocations}";
        }
        else if (jobRequest.GenerateHierarchicalAssessments && !jobRequest.EnableHierarchicalProcessing)
        {
            return "Connectivity assessment job has been queued for processing. Hierarchical processing disabled for performance.";
        }
        else
        {
            return "Connectivity assessment job has been queued for processing.";
        }
    }

    /// <summary>
    /// Get default maximum hierarchical locations based on location type
    /// </summary>
    private static int GetDefaultMaxHierarchicalLocations(LocationTypeEnum locationType)
    {
        return locationType switch
        {
            LocationTypeEnum.Regional => 500,      // Regions can have many children, limit for performance
            LocationTypeEnum.Provincial => 800,    // Provinces are more manageable
            LocationTypeEnum.Municipality => 1000, // Municipalities have barangays, can handle more
            LocationTypeEnum.Barangay => 0,        // Barangays have no children
            _ => 1000
        };
    }

    /// <summary>
    /// Get default hierarchical batch size based on location type
    /// </summary>
    private static int GetDefaultHierarchicalBatchSize(LocationTypeEnum locationType)
    {
        return locationType switch
        {
            LocationTypeEnum.Regional => 25,      // Smaller batches for complex regional processing
            LocationTypeEnum.Provincial => 40,    // Medium batches for provinces
            LocationTypeEnum.Municipality => 60,  // Larger batches for municipalities
            LocationTypeEnum.Barangay => 100,     // Largest batches for barangays (simpler)
            _ => 50
        };
    }

    /// <summary>
    /// Calculate refined estimated total locations that will be processed based on limits and configuration
    /// This provides a more accurate estimate than the basic calculation by considering processing limits
    /// </summary>
    private static int CalculateRefinedEstimatedTotalLocations(ConnectivityAssessmentJobRequestDto jobRequest)
    {
        var primaryLocations = jobRequest.LocationIds.Count;

        if (!jobRequest.GenerateHierarchicalAssessments || !jobRequest.EnableHierarchicalProcessing)
        {
            return primaryLocations;
        }

        // Use more realistic estimates based on typical Philippine administrative structure
        var estimatedChildrenPerLocation = jobRequest.LocationType switch
        {
            LocationTypeEnum.Regional when jobRequest.HierarchicalDepth >= 3 => 800,  // Region -> all provinces -> all municipalities -> all barangays
            LocationTypeEnum.Regional when jobRequest.HierarchicalDepth >= 2 => 80,   // Region -> all provinces -> all municipalities
            LocationTypeEnum.Regional => 15,                                          // Region -> provinces only
            LocationTypeEnum.Provincial when jobRequest.HierarchicalDepth >= 2 => 300, // Province -> all municipalities -> all barangays
            LocationTypeEnum.Provincial => 25,                                        // Province -> municipalities only
            LocationTypeEnum.Municipality => 15,                                      // Municipality -> barangays (more realistic average)
            _ => 0
        };

        var estimatedHierarchicalLocations = primaryLocations * estimatedChildrenPerLocation;
        var cappedEstimatedHierarchical = Math.Min(estimatedHierarchicalLocations, jobRequest.MaxHierarchicalLocations);

        return primaryLocations + cappedEstimatedHierarchical;
    }

    /// <summary>
    /// Calculate estimated total number of locations that will be processed (kept for backward compatibility)
    /// This is the basic estimation without considering processing limits or refined configuration
    /// </summary>
    private static int CalculateEstimatedTotalLocations(StartConnectivityAssessmentJobRequest request)
    {
        if (!request.GenerateHierarchicalAssessments)
        {
            return request.LocationIds.Count;
        }

        // Use more realistic estimates based on typical Philippine administrative structure
        var estimatedChildrenPerLocation = request.LocationType switch
        {
            LocationTypeEnum.Regional when request.HierarchicalDepth >= 3 => 800,  // Region -> Provinces -> Municipalities -> Barangays
            LocationTypeEnum.Regional when request.HierarchicalDepth >= 2 => 80,   // Region -> Provinces -> Municipalities
            LocationTypeEnum.Regional => 15,                                       // Region -> Provinces only
            LocationTypeEnum.Provincial when request.HierarchicalDepth >= 2 => 300, // Province -> Municipalities -> Barangays
            LocationTypeEnum.Provincial => 25,                                     // Province -> Municipalities only
            LocationTypeEnum.Municipality => 15,                                   // Municipality -> Barangays (more realistic average)
            _ => 0
        };

        return request.LocationIds.Count + (request.LocationIds.Count * estimatedChildrenPerLocation);
    }

    /// <summary>
    /// Estimate processing time based on estimated job configuration
    /// </summary>
    private static int EstimateProcessingTime(ConnectivityAssessmentJobRequestDto jobRequest)
    {
        // Base time per location (in minutes)
        var baseTimePerLocation = jobRequest.ForceRecalculate ? 0.5 : 0.2;
        
        // Calculate estimated locations to process
        var estimatedLocations = CalculateRefinedEstimatedTotalLocations(jobRequest);
        
        var estimatedMinutes = (int)Math.Ceiling(estimatedLocations * baseTimePerLocation);
        
        // Minimum 1 minute, maximum 180 minutes for display purposes
        return Math.Max(1, Math.Min(180, estimatedMinutes));
    }
}