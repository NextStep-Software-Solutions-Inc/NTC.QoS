using MediatR;
using Microsoft.EntityFrameworkCore;
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
    /// Estimation factors for job duration
    /// </summary>
    public static class EstimationFactors
    {
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
/// 
/// KEY FEATURE: Calculates ACTUAL total location counts from database BEFORE job execution
/// - Queries region/province/municipality repositories for real child location counts
/// - Applies hierarchical depth and max location limits
/// - Passes accurate ActualTotalLocations to ProcessConnectivityAssessmentJob
/// - Ensures complete and accurate progress tracking throughout job lifecycle
/// </summary>
public class StartConnectivityAssessmentJobHandler : IRequestHandler<StartConnectivityAssessmentJobRequest, ApiResponse<ConnectivityAssessmentJobResponseDto>>
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<StartConnectivityAssessmentJobHandler> _logger;
    private readonly IUserContextService _userContextService;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private static int _jobIdCounter = ConnectivityAssessmentJobConfiguration.JobIdCounterStart;

    public StartConnectivityAssessmentJobHandler(
        IBackgroundJobService backgroundJobService,
        IMemoryCache memoryCache,
        ILogger<StartConnectivityAssessmentJobHandler> logger,
        IUserContextService userContextService,
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository)
    {
        _backgroundJobService = backgroundJobService;
        _memoryCache = memoryCache;
        _logger = logger;
        _userContextService = userContextService;
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
    }

    public async Task<ApiResponse<ConnectivityAssessmentJobResponseDto>> Handle(
        StartConnectivityAssessmentJobRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Initialize job
            var jobId = Interlocked.Increment(ref _jobIdCounter);
            var startTime = DateTime.UtcNow;
            var currentUserId = _userContextService.GetCurrentUserId() ?? "System";

            // Step 2: Calculate ACTUAL location counts from database FIRST
            var actualTotalLocations = await CalculateActualTotalLocationsAsync(
                request.LocationIds,
                request.LocationType,
                request.GenerateHierarchicalAssessments,
                request.HierarchicalDepth,
                request.MaxHierarchicalLocations ?? int.MaxValue,
                cancellationToken);

            // Step 3: Create job request with actual total locations
            var jobRequest = CreateJobRequest(jobId, request, currentUserId, startTime, actualTotalLocations);

            // Step 4: Log configuration
            LogJobConfiguration(jobId, jobRequest);

            // Step 5: Log location count details
            LogLocationCounts(jobId, jobRequest, actualTotalLocations);

            // Step 6: Initialize cache with actual counts
            InitializeJobStatusCache(jobId, actualTotalLocations, startTime);

            // Step 7: Enqueue job
            var hangfireJobId = _backgroundJobService.Enqueue<IProcessConnectivityAssessmentJob>(
                job => job.ExecuteAsync(jobRequest, CancellationToken.None));

            // Step 8: Log job start
            LogJobStart(jobId, hangfireJobId, request, jobRequest, actualTotalLocations);

            // Step 9: Create and return response
            var response = CreateJobResponse(jobId, jobRequest, actualTotalLocations);

            return await Task.FromResult(new ApiResponse<ConnectivityAssessmentJobResponseDto>
            {
                Succeeded = true,
                Message = $"Background job started successfully. Job ID: {jobId}",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting background connectivity assessment job");
            return await Task.FromResult(new ApiResponse<ConnectivityAssessmentJobResponseDto>
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
        DateTime startTime,
        int actualTotalLocations)
    {
        return new ConnectivityAssessmentJobRequestDto
        {
            JobId = jobId,
            LocationIds = request.LocationIds,
            LocationType = request.LocationType,
            ForceRecalculate = request.ForceRecalculate,
            GenerateHierarchicalAssessments = request.GenerateHierarchicalAssessments,
            HierarchicalDepth = request.HierarchicalDepth,
            MaxHierarchicalLocations = request.MaxHierarchicalLocations ?? int.MaxValue,
            HierarchicalBatchSize = request.HierarchicalBatchSize ??
                GetDefaultHierarchicalBatchSize(request.LocationType),
            ActualTotalLocations = actualTotalLocations,
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
            TotalCount = estimatedLocations
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
        int actualTotalLocations)
    {
        return new ConnectivityAssessmentJobResponseDto
        {
            JobId = jobId,
            Status = ConnectivityAssessmentJobStatusEnum.Pending,
            Message = GetJobQueueMessage(jobRequest),
            EstimatedDurationMinutes = EstimateProcessingTime(jobRequest, actualTotalLocations),
            EstimatedTotalLocations = actualTotalLocations
        };
    }

    /// <summary>
    /// Logs job configuration for transparency
    /// </summary>
    private void LogJobConfiguration(int jobId, ConnectivityAssessmentJobRequestDto jobRequest)
    {
        _logger.LogInformation(
            "Job {JobId} configuration - Hierarchical: {Hierarchical}, " +
            "Max locations: {MaxLocations}, Batch size: {BatchSize}",
            jobId,
            jobRequest.GenerateHierarchicalAssessments,
            jobRequest.MaxHierarchicalLocations,
            jobRequest.HierarchicalBatchSize);
    }

    /// <summary>
    /// Logs location count details for debugging
    /// </summary>
    private void LogLocationCounts(int jobId, ConnectivityAssessmentJobRequestDto jobRequest, int actualTotalLocations)
    {
        if (!jobRequest.GenerateHierarchicalAssessments)
            return;

        var primaryCount = jobRequest.LocationIds.Count;
        var actualChildrenCount = actualTotalLocations - primaryCount;
        var hasLimit = jobRequest.MaxHierarchicalLocations < int.MaxValue;
        var limitInfo = hasLimit ? $", limited to: {jobRequest.MaxHierarchicalLocations}" : ", no limit (processing all)";

        _logger.LogInformation(
            "Job {JobId} actual location counts - Primary: {Primary}, Actual children: {ActualChildren}, " +
            "Total: {Total}, LocationType: {LocationType}, Depth: {Depth}{LimitInfo}",
            jobId, primaryCount, actualChildrenCount, actualTotalLocations,
            jobRequest.LocationType, jobRequest.HierarchicalDepth, limitInfo);
    }

    /// <summary>
    /// Logs job start information
    /// </summary>
    private void LogJobStart(
        int jobId,
        string hangfireJobId,
        StartConnectivityAssessmentJobRequest request,
        ConnectivityAssessmentJobRequestDto jobRequest,
        int actualTotalLocations)
    {
        _logger.LogInformation(
            "Started background connectivity assessment job {JobId} (Hangfire: {HangfireJobId}) for {LocationCount} {LocationType}s. " +
            "Hierarchical: {Hierarchical}, Force: {Force}, ActualTotal: {ActualTotal}",
            jobId, hangfireJobId, request.LocationIds.Count, request.LocationType,
            request.GenerateHierarchicalAssessments,
            request.ForceRecalculate, actualTotalLocations);
    }

    /// <summary>
    /// Get appropriate message based on hierarchical configuration
    /// </summary>
    private static string GetJobQueueMessage(ConnectivityAssessmentJobRequestDto jobRequest)
    {
        if (jobRequest.GenerateHierarchicalAssessments)
        {
            var limitMsg = jobRequest.MaxHierarchicalLocations < int.MaxValue
                ? $"Max hierarchical locations: {jobRequest.MaxHierarchicalLocations}"
                : "Processing all hierarchical locations";
            return $"Connectivity assessment job (with hierarchical processing) has been queued for processing. {limitMsg}";
        }
        else
        {
            return "Connectivity assessment job has been queued for processing.";
        }
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
    /// Estimate processing time based on actual job configuration
    /// </summary>
    private static int EstimateProcessingTime(ConnectivityAssessmentJobRequestDto jobRequest, int actualLocations)
    {
        var baseTimePerLocation = jobRequest.ForceRecalculate
            ? ConnectivityAssessmentJobConfiguration.EstimationFactors.BaseTimeForceRecalculate
            : ConnectivityAssessmentJobConfiguration.EstimationFactors.BaseTimeNormal;

        var estimatedMinutes = (int)Math.Ceiling(actualLocations * baseTimePerLocation);

        return Math.Max(
            ConnectivityAssessmentJobConfiguration.EstimationFactors.MinEstimatedMinutes,
            Math.Min(ConnectivityAssessmentJobConfiguration.EstimationFactors.MaxEstimatedMinutes, estimatedMinutes));
    }

    /// <summary>
    /// Calculates the actual total number of locations to be processed based on database counts
    /// This replaces the estimation-based approach with actual hierarchical counts from the database
    /// </summary>
    private async Task<int> CalculateActualTotalLocationsAsync(
        List<Guid> locationIds,
        LocationTypeEnum locationType,
        bool generateHierarchical,
        int hierarchicalDepth,
        int maxHierarchicalLocations,
        CancellationToken cancellationToken)
    {
        var primaryCount = locationIds.Count;

        // If hierarchical processing is not enabled, return only primary count
        if (!generateHierarchical)
        {
            _logger.LogInformation("Hierarchical processing disabled. Total locations: {Count}", primaryCount);
            return primaryCount;
        }

        // Get actual child location counts from database
        var childLocationCounts = await GetActualChildLocationCountsAsync(
            locationIds,
            locationType,
            hierarchicalDepth,
            cancellationToken);

        // Apply the max hierarchical locations limit
        var cappedChildCount = Math.Min(childLocationCounts, maxHierarchicalLocations);

        var totalLocations = primaryCount + cappedChildCount;

        _logger.LogInformation(
            "Actual location counts - Primary: {Primary}, Children: {Children} (capped: {Capped}, max: {Max}), Total: {Total}",
            primaryCount, childLocationCounts, cappedChildCount, maxHierarchicalLocations, totalLocations);

        return totalLocations;
    }

    /// <summary>
    /// Gets the actual count of child locations from the database based on parent locations and depth
    /// </summary>
    private async Task<int> GetActualChildLocationCountsAsync(
        List<Guid> locationIds,
        LocationTypeEnum locationType,
        int hierarchicalDepth,
        CancellationToken cancellationToken)
    {
        int totalChildCount = 0;

        foreach (var locationId in locationIds)
        {
            var childCount = await GetChildLocationCountForSingleLocationAsync(
                locationId,
                locationType,
                hierarchicalDepth,
                cancellationToken);

            totalChildCount += childCount;
        }

        return totalChildCount;
    }

    /// <summary>
    /// Gets the actual count of child locations for a single parent location
    /// </summary>
    private async Task<int> GetChildLocationCountForSingleLocationAsync(
        Guid locationId,
        LocationTypeEnum locationType,
        int hierarchicalDepth,
        CancellationToken cancellationToken)
    {
        return locationType switch
        {
            LocationTypeEnum.Regional => await _regionRepository.GetChildLocationCountAsync(locationId, hierarchicalDepth, cancellationToken),
            LocationTypeEnum.Provincial => await _provinceRepository.GetChildLocationCountAsync(locationId, hierarchicalDepth, cancellationToken),
            LocationTypeEnum.Municipality => await _municipalityRepository.GetChildLocationCountAsync(locationId, cancellationToken),
            LocationTypeEnum.Barangay => 0, // Barangays have no children
            _ => 0
        };
    }
}