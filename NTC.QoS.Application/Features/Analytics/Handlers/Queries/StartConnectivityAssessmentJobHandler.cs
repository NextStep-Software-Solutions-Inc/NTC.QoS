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
/// Configuration constants for connectivity assessment job processing
/// Based on Philippine administrative structure data
/// </summary>
public static class ConnectivityAssessmentJobConfiguration
{
    /// <summary>
    /// Maximum locations to process hierarchically by location type
    /// Values set to ensure complete processing of all Philippine administrative levels
    /// </summary>
    public static class MaxHierarchicalLocations
    {
        /// <summary>
        /// Regional limit: Handles worst case (Eastern Visayas: 143 munis + 4365 barangays = 4508)
        /// Set to 5000 to ensure complete processing of any single region
        /// </summary>
        public const int Regional = 5000;

        /// <summary>
        /// Provincial limit: Handles worst case (Calabarzon: 142 munis + 4020 barangays = 4162)
        /// Set to 4500 to ensure complete processing of any single province
        /// </summary>
        public const int Provincial = 4500;

        /// <summary>
        /// Municipal limit: Conservative estimate based on data (~50-200 barangays per municipality)
        /// Set to 250 to provide safe headroom for largest municipalities
        /// </summary>
        public const int Municipality = 250;

        /// <summary>
        /// Barangay limit: Barangays have no children
        /// </summary>
        public const int Barangay = 0;

        /// <summary>
        /// Default limit for unknown location types
        /// </summary>
        public const int Default = 5000;
    }

    /// <summary>
    /// Batch sizes for hierarchical processing
    /// Optimized for throughput while maintaining stability
    /// </summary>
    public static class HierarchicalBatchSize
    {
        /// <summary>
        /// Regional batch size: Higher throughput for large-scale regional processing
        /// </summary>
        public const int Regional = 100;

        /// <summary>
        /// Provincial batch size: Consistent batch size for provincial processing
        /// </summary>
        public const int Provincial = 100;

        /// <summary>
        /// Municipal batch size: Larger batches for simpler barangay processing
        /// </summary>
        public const int Municipality = 150;

        /// <summary>
        /// Barangay batch size: Largest batches for barangays (no children)
        /// </summary>
        public const int Barangay = 150;

        /// <summary>
        /// Default batch size for unknown location types
        /// </summary>
        public const int Default = 100;
    }

    /// <summary>
    /// Estimation factors for job duration and location counts
    /// Based on typical Philippine administrative structure
    /// </summary>
    public static class EstimationFactors
    {
        /// <summary>
        /// Estimated children per location for different depths and types
        /// </summary>
        public static class ChildrenPerLocation
        {
            /// <summary>
            /// Region -> all provinces -> all municipalities -> all barangays
            /// </summary>
            public const int RegionalDepth3 = 800;

            /// <summary>
            /// Region -> all provinces -> all municipalities
            /// </summary>
            public const int RegionalDepth2 = 80;

            /// <summary>
            /// Region -> provinces only
            /// </summary>
            public const int RegionalDepth1 = 15;

            /// <summary>
            /// Province -> all municipalities -> all barangays
            /// </summary>
            public const int ProvincialDepth2 = 300;

            /// <summary>
            /// Province -> municipalities only
            /// </summary>
            public const int ProvincialDepth1 = 25;

            /// <summary>
            /// Municipality -> barangays (more realistic average)
            /// </summary>
            public const int Municipality = 15;
        }

        /// <summary>
        /// Time estimates per location (in minutes)
        /// </summary>
        public const double BaseTimeForceRecalculate = 0.5;
        public const double BaseTimeNormal = 0.2;

        /// <summary>
        /// Duration limits for display purposes
        /// </summary>
        public const int MinEstimatedMinutes = 1;
        public const int MaxEstimatedMinutes = 180;
    }

    /// <summary>
    /// Cache configuration
    /// </summary>
    public const int JobStatusCacheHours = 24;

    /// <summary>
    /// Job ID counter start value for easier identification
    /// </summary>
    public const int JobIdCounterStart = 1000;
}

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
    private static int _jobIdCounter = ConnectivityAssessmentJobConfiguration.JobIdCounterStart;

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
            // Step 1: Initialize job
            var jobId = Interlocked.Increment(ref _jobIdCounter);
            var startTime = DateTime.UtcNow;
            var currentUserId = _userContextService.GetCurrentUserId() ?? "System";

            // Step 2: Create job request
            var jobRequest = CreateJobRequest(jobId, request, currentUserId, startTime);

            // Step 3: Log configuration
            LogJobConfiguration(jobId, jobRequest);

            // Step 4: Calculate estimates
            var refinedEstimatedLocations = CalculateEstimatedTotalLocations(
                jobRequest.LocationIds.Count,
                jobRequest.LocationType,
                jobRequest.GenerateHierarchicalAssessments,
                jobRequest.EnableHierarchicalProcessing,
                jobRequest.HierarchicalDepth,
                jobRequest.MaxHierarchicalLocations);

            // Step 5: Log estimation details
            LogLocationEstimation(jobId, jobRequest, refinedEstimatedLocations);

            // Step 6: Initialize cache
            InitializeJobStatusCache(jobId, refinedEstimatedLocations, startTime);

            // Step 7: Enqueue job
            var hangfireJobId = _backgroundJobService.Enqueue<IProcessConnectivityAssessmentJob>(
                job => job.ExecuteAsync(jobRequest, CancellationToken.None));

            // Step 8: Log job start
            LogJobStart(jobId, hangfireJobId, request, jobRequest, refinedEstimatedLocations);

            // Step 9: Create and return response
            var response = CreateJobResponse(jobId, jobRequest, refinedEstimatedLocations);

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
    /// Creates the job request DTO with all necessary configuration
    /// </summary>
    private ConnectivityAssessmentJobRequestDto CreateJobRequest(
        int jobId,
        StartConnectivityAssessmentJobRequest request,
        string userId,
        DateTime startTime)
    {
        return new ConnectivityAssessmentJobRequestDto
        {
            JobId = jobId,
            LocationIds = request.LocationIds,
            LocationType = request.LocationType,
            ForceRecalculate = request.ForceRecalculate,
            GenerateHierarchicalAssessments = request.GenerateHierarchicalAssessments,
            HierarchicalDepth = request.HierarchicalDepth,
            EnableHierarchicalProcessing = request.EnableHierarchicalProcessing ?? false,
            MaxHierarchicalLocations = request.MaxHierarchicalLocations ??
                GetDefaultMaxHierarchicalLocations(request.LocationType),
            HierarchicalBatchSize = request.HierarchicalBatchSize ??
                GetDefaultHierarchicalBatchSize(request.LocationType),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UserId = userId,
            RequestedAt = startTime
        };
    }

    /// <summary>
    /// Initializes job status in memory cache
    /// </summary>
    private void InitializeJobStatusCache(int jobId, int estimatedLocations, DateTime startTime)
    {
        var initialJobStatus = new ConnectivityAssessmentJobStatusDto
        {
            JobId = jobId,
            Status = ConnectivityAssessmentJobStatusEnum.Pending,
            Message = "Job is queued and waiting to start",
            LastUpdated = startTime,
            StartTime = null,
            EndTime = null,
            ProcessedCount = 0,
            EstimatedTotalCount = estimatedLocations
        };

        var cacheKey = $"job_status_{jobId}";
        var cacheExpiration = TimeSpan.FromHours(ConnectivityAssessmentJobConfiguration.JobStatusCacheHours);
        _memoryCache.Set(cacheKey, initialJobStatus, cacheExpiration);
    }

    /// <summary>
    /// Creates the job response DTO
    /// </summary>
    private static ConnectivityAssessmentJobResponseDto CreateJobResponse(
        int jobId,
        ConnectivityAssessmentJobRequestDto jobRequest,
        int refinedEstimatedLocations)
    {
        return new ConnectivityAssessmentJobResponseDto
        {
            JobId = jobId,
            Status = ConnectivityAssessmentJobStatusEnum.Pending,
            Message = GetJobQueueMessage(jobRequest),
            EstimatedDurationMinutes = EstimateProcessingTime(jobRequest, refinedEstimatedLocations),
            EstimatedTotalLocations = refinedEstimatedLocations
        };
    }

    /// <summary>
    /// Logs job configuration for transparency
    /// </summary>
    private void LogJobConfiguration(int jobId, ConnectivityAssessmentJobRequestDto jobRequest)
    {
        _logger.LogInformation(
            "Job {JobId} configuration - Hierarchical requested: {Requested}, Enabled: {Enabled}, " +
            "Max locations: {MaxLocations}, Batch size: {BatchSize}",
            jobId,
            jobRequest.GenerateHierarchicalAssessments,
            jobRequest.EnableHierarchicalProcessing,
            jobRequest.MaxHierarchicalLocations,
            jobRequest.HierarchicalBatchSize);
    }

    /// <summary>
    /// Logs location estimation details for debugging
    /// </summary>
    private void LogLocationEstimation(int jobId, ConnectivityAssessmentJobRequestDto jobRequest, int refinedEstimatedLocations)
    {
        if (!jobRequest.GenerateHierarchicalAssessments || !jobRequest.EnableHierarchicalProcessing)
            return;

        var primaryCount = jobRequest.LocationIds.Count;
        var estimatedChildrenCount = refinedEstimatedLocations - primaryCount;

        _logger.LogInformation(
            "Job {JobId} location estimation - Primary: {Primary}, Estimated children: {EstimatedChildren}, " +
            "Total estimated: {TotalEstimated}, LocationType: {LocationType}, Depth: {Depth}",
            jobId, primaryCount, estimatedChildrenCount, refinedEstimatedLocations,
            jobRequest.LocationType, jobRequest.HierarchicalDepth);
    }

    /// <summary>
    /// Logs job start information
    /// </summary>
    private void LogJobStart(
        int jobId,
        string hangfireJobId,
        StartConnectivityAssessmentJobRequest request,
        ConnectivityAssessmentJobRequestDto jobRequest,
        int refinedEstimatedLocations)
    {
        _logger.LogInformation(
            "Started background connectivity assessment job {JobId} (Hangfire: {HangfireJobId}) for {LocationCount} {LocationType}s. " +
            "Hierarchical requested: {Hierarchical}, Enabled: {Enabled}, Force: {Force}, EstimatedTotal: {EstimatedTotal}",
            jobId, hangfireJobId, request.LocationIds.Count, request.LocationType,
            request.GenerateHierarchicalAssessments, jobRequest.EnableHierarchicalProcessing,
            request.ForceRecalculate, refinedEstimatedLocations);
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
    /// Configured to handle complete processing of all Philippine administrative levels
    /// </summary>
    private static int GetDefaultMaxHierarchicalLocations(LocationTypeEnum locationType)
    {
        return locationType switch
        {
            LocationTypeEnum.Regional => ConnectivityAssessmentJobConfiguration.MaxHierarchicalLocations.Regional,
            LocationTypeEnum.Provincial => ConnectivityAssessmentJobConfiguration.MaxHierarchicalLocations.Provincial,
            LocationTypeEnum.Municipality => ConnectivityAssessmentJobConfiguration.MaxHierarchicalLocations.Municipality,
            LocationTypeEnum.Barangay => ConnectivityAssessmentJobConfiguration.MaxHierarchicalLocations.Barangay,
            _ => ConnectivityAssessmentJobConfiguration.MaxHierarchicalLocations.Default
        };
    }

    /// <summary>
    /// Get default hierarchical batch size based on location type
    /// Optimized for throughput while maintaining stability
    /// </summary>
    private static int GetDefaultHierarchicalBatchSize(LocationTypeEnum locationType)
    {
        return locationType switch
        {
            LocationTypeEnum.Regional => ConnectivityAssessmentJobConfiguration.HierarchicalBatchSize.Regional,
            LocationTypeEnum.Provincial => ConnectivityAssessmentJobConfiguration.HierarchicalBatchSize.Provincial,
            LocationTypeEnum.Municipality => ConnectivityAssessmentJobConfiguration.HierarchicalBatchSize.Municipality,
            LocationTypeEnum.Barangay => ConnectivityAssessmentJobConfiguration.HierarchicalBatchSize.Barangay,
            _ => ConnectivityAssessmentJobConfiguration.HierarchicalBatchSize.Default
        };
    }

    /// <summary>
    /// Gets the estimated children per location based on type and hierarchical depth
    /// Uses realistic estimates based on typical Philippine administrative structure
    /// </summary>
    private static int GetEstimatedChildrenPerLocation(LocationTypeEnum locationType, int hierarchicalDepth)
    {
        return locationType switch
        {
            LocationTypeEnum.Regional when hierarchicalDepth >= 3 =>
                ConnectivityAssessmentJobConfiguration.EstimationFactors.ChildrenPerLocation.RegionalDepth3,
            LocationTypeEnum.Regional when hierarchicalDepth >= 2 =>
                ConnectivityAssessmentJobConfiguration.EstimationFactors.ChildrenPerLocation.RegionalDepth2,
            LocationTypeEnum.Regional =>
                ConnectivityAssessmentJobConfiguration.EstimationFactors.ChildrenPerLocation.RegionalDepth1,
            LocationTypeEnum.Provincial when hierarchicalDepth >= 2 =>
                ConnectivityAssessmentJobConfiguration.EstimationFactors.ChildrenPerLocation.ProvincialDepth2,
            LocationTypeEnum.Provincial =>
                ConnectivityAssessmentJobConfiguration.EstimationFactors.ChildrenPerLocation.ProvincialDepth1,
            LocationTypeEnum.Municipality =>
                ConnectivityAssessmentJobConfiguration.EstimationFactors.ChildrenPerLocation.Municipality,
            _ => 0
        };
    }

    /// <summary>
    /// Calculate estimated total locations with optional processing limits
    /// This unified method provides accurate estimates by considering processing limits
    /// </summary>
    private static int CalculateEstimatedTotalLocations(
        int primaryLocationCount,
        LocationTypeEnum locationType,
        bool generateHierarchical,
        bool enableHierarchicalProcessing,
        int hierarchicalDepth,
        int maxHierarchicalLocations)
    {
        if (!generateHierarchical || !enableHierarchicalProcessing)
        {
            return primaryLocationCount;
        }

        var estimatedChildrenPerLocation = GetEstimatedChildrenPerLocation(locationType, hierarchicalDepth);
        var estimatedHierarchicalLocations = primaryLocationCount * estimatedChildrenPerLocation;
        var cappedEstimatedHierarchical = Math.Min(estimatedHierarchicalLocations, maxHierarchicalLocations);

        return primaryLocationCount + cappedEstimatedHierarchical;
    }

    /// <summary>
    /// Estimate processing time based on estimated job configuration
    /// </summary>
    private static int EstimateProcessingTime(ConnectivityAssessmentJobRequestDto jobRequest, int estimatedLocations)
    {
        var baseTimePerLocation = jobRequest.ForceRecalculate
            ? ConnectivityAssessmentJobConfiguration.EstimationFactors.BaseTimeForceRecalculate
            : ConnectivityAssessmentJobConfiguration.EstimationFactors.BaseTimeNormal;

        var estimatedMinutes = (int)Math.Ceiling(estimatedLocations * baseTimePerLocation);

        return Math.Max(
            ConnectivityAssessmentJobConfiguration.EstimationFactors.MinEstimatedMinutes,
            Math.Min(ConnectivityAssessmentJobConfiguration.EstimationFactors.MaxEstimatedMinutes, estimatedMinutes));
    }
}