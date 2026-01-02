// =============================================================================
// NTC Quality of Service Platform
// Connectivity Assessment Background Job
// =============================================================================
// 
// PURPOSE:
// Processes connectivity assessments for geographic locations using optimized
// spatial queries to calculate infrastructure counts, speed statistics, and
// CMTS scores without blocking API requests.
//
// KEY OPTIMIZATIONS:
// - Separate actual asset counts from provider distribution data
// - Subquery-based filtering for Regional/Provincial (no boundary materialization)
// - Envelope filtering for Municipal/Barangay (single boundary materialization)
// - Batch processing with dynamic batch sizes (1-50 per batch)
// - Multi-level caching (infrastructure, speed, technology)
// - Memory management for long-running jobs
//
// PROCESS FLOW:
// 1. Extract location IDs and types from job request
// 2. Discover hierarchical child locations if enabled
// 3. Process locations in batches with deferred persistence
// 4. Calculate actual infrastructure counts using COUNT queries
// 5. Calculate CMTS scores (5 components, 0-15 total points)
// 6. Save batches in transactions with verification
// 7. Report progress and update job status
//
// INFRASTRUCTURE COUNTING:
// - Cell towers: Actual COUNT(*) from optimized spatial queries
// - NAPs (fiber): Actual COUNT(*) from optimized spatial queries  
// - WiFi hotspots: Actual COUNT(*) from optimized spatial queries
// - Provider names: Distinct values for provider breakdown
// - No approximation - all counts are accurate database queries
//
// CMTS SCORING (0-15 points):
// 1. Fiber Coverage (0-3): Fiber infrastructure availability
// 2. Internet Access (0-3): Overall infrastructure availability
// 3. Technology Type (0-3): Technology generation mix (5G/4G/3G/2G)
// 4. Quality of Service (0-3): Speed test performance
// 5. Coverage Availability (0-3): Infrastructure density vs area/population
//
// =============================================================================

using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Infrastructure.Helper;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job for processing connectivity assessments using optimized spatial queries.
/// 
/// =============================================================================
/// MAIN RESPONSIBILITIES
/// =============================================================================
/// 1. Execute connectivity assessments for geographic locations (Region to Barangay)
/// 2. Calculate accurate infrastructure asset counts using optimized queries
/// 3. Compute CMTS scores (Connectivity Maturity and Technology Score)
/// 4. Process hierarchical location structures efficiently
/// 5. Track progress and report status in real-time
/// 
/// =============================================================================
/// INFRASTRUCTURE COUNTING STRATEGY
/// =============================================================================
/// ACCURATE COUNTS (via COUNT(*) queries):
/// - actualCellCount: Total cell tower/sector count
/// - actualNapCount: Total fiber NAP count
/// - actualWifiCount: Total WiFi hotspot count
/// 
/// PROVIDER DISTRIBUTION (via DISTINCT provider names):
/// - cellProviders: Distinct provider names from cells
/// - napProviders: Distinct provider names from NAPs
/// - wifiProviders: Distinct provider names from WiFi
/// 
/// =============================================================================
/// QUERY OPTIMIZATION BY LOCATION TYPE
/// =============================================================================
/// REGIONAL & PROVINCIAL (Large Areas):
/// - Uses: Subquery-based boundary filtering
/// - Benefit: No geometry materialization, prevents timeout
/// - Method: GeoQueryHelper.ApplyBoundaryFilterAsync()
/// - Performance: Handles complex boundaries with 1K-4K child locations
/// 
/// MUNICIPAL & BARANGAY (Small Areas):
/// - Uses: Envelope filtering with single boundary load
/// - Benefit: Faster than subquery for small geometries
/// - Method: GeoQueryHelper.ApplyEnvelopeFilter()
/// - Performance: Optimal for 10-50 child locations
/// 
/// =============================================================================
/// BATCH PROCESSING CONFIGURATION
/// =============================================================================
/// Dynamic batch sizes optimize transaction size vs processing speed:
/// - Regional: 1 per batch (very large spatial queries)
/// - Provincial: 3 per batch (reduced for stability, timeout prevention)
/// - Municipal: 10 per batch (reduced for faster commits, timeout prevention)
/// - Barangay: 20 per batch (reduced to prevent timeout, improve reliability)
/// 
/// =============================================================================
/// JOB CONFIGURATION
/// =============================================================================
/// Queue: "connectivity-assessment" (serialized execution)
/// Retry: None (Attempts = 0, fail immediately)
/// Timeout: 4 hours (14,400 seconds)
/// Concurrency: Disabled (prevents database exhaustion)
/// </summary>
[Queue("connectivity-assessment")]
[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
[DisableConcurrentExecution(timeoutInSeconds: 14400)] // 4 hours for large hierarchical jobs
public class ProcessConnectivityAssessmentJob : IProcessConnectivityAssessmentJob
{
    private readonly ILogger<ProcessConnectivityAssessmentJob> _logger;
    private readonly IConnectivityAssessmentRepository _connectivityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<Province> _provinceRepository;
    private readonly IRepository<Region> _regionRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ICellRepository _cellRepository;
    private readonly ICellSectorRepository _cellSectorRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly INAPRepository _napRepository;
    private readonly IWiFiHotspotRepository _wifiRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IWirelessValidationRepository _wirelessValidationRepository;
    private readonly IWirelessCoverageComputationService _wirelessCoverageService;

    // Store the current user ID for the job execution
    private string _currentUserId = "System";

    // Dynamic batch sizing based on location type and hierarchy level
    // Regional: 1 per batch (each region has 1K-4K child assessments, save per region)
    // Provincial: 3 per batch (reduced from 5 for stability)
    // Municipality: 10 per batch (reduced from 25 for faster commits)
    // Barangay: 20 per batch (reduced from 50 to prevent timeout)
    private const int REGIONAL_BATCH_SIZE = 1;
    private const int PROVINCIAL_BATCH_SIZE = 1;
    private const int MUNICIPALITY_BATCH_SIZE = 1;
    private const int BARANGAY_BATCH_SIZE = 3;
    
    private const int PROGRESS_REPORT_INTERVAL = 5; // Report progress every 5 items for smoother updates
    private const int MEMORY_CHECK_INTERVAL = 5; // Check memory every 5 batches
    private const int RECENT_ASSESSMENT_DAYS = 30; // Consider assessments within 30 days as recent
    
    // Database verification configuration
    private const int VERIFICATION_BATCH_SIZE = 50; // Process location IDs in batches to prevent timeout
    private const int VERIFICATION_TIMEOUT_MINUTES = 60; // Generous timeout for database verification (5 minutes)
    private const int WIRELESS_COMPUTATION_TIMEOUT_MINUTES = 10;
    private const int BATCH_SAVE_TIMEOUT_SECONDS = 300;

    public ProcessConnectivityAssessmentJob(
        ILogger<ProcessConnectivityAssessmentJob> logger,
        IConnectivityAssessmentRepository connectivityRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<Province> provinceRepository,
        IRepository<Region> regionRepository,
        IMemoryCache memoryCache,
        ICellRepository cellRepository,
        ICellSectorRepository cellSectorRepository,
        ICellularRepository cellularRepository,
        INAPRepository napRepository,
        IWiFiHotspotRepository wifiRepository,
        ISpeedtestRepository speedtestRepository,
        IWirelessValidationRepository wirelessValidationRepository,
        IWirelessCoverageComputationService wirelessCoverageService)
    {
        _logger = logger;
        _connectivityRepository = connectivityRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _memoryCache = memoryCache;
        _cellRepository = cellRepository;
        _cellSectorRepository = cellSectorRepository;
        _cellularRepository = cellularRepository;
        _napRepository = napRepository;
        _wifiRepository = wifiRepository;
        _speedtestRepository = speedtestRepository;
        _wirelessValidationRepository = wirelessValidationRepository;
        _wirelessCoverageService = wirelessCoverageService;
    }

    /// <summary>
    /// Executes the connectivity assessment job for specified locations.
    /// 
    /// PROCESS FLOW:
    /// 1. Extract location IDs and types from request
    /// 2. Process hierarchical child locations if enabled
    /// 3. Process primary locations with batch optimization
    /// 4. Verify database persistence
    /// 5. Update job status to completed
    /// 
    /// No automatic retry - failures are logged and job status updated to Failed.
    /// </summary>
    /// <param name="jobRequest">Job configuration including locations, hierarchy settings, and filters</param>
    /// <param name="cancellationToken">Cancellation token for job termination</param>
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(ConnectivityAssessmentJobRequestDto jobRequest, CancellationToken cancellationToken)
    {
        var jobId = jobRequest.JobId; // Hangfire job ID
        var startTime = DateTime.UtcNow;

        // Store the user ID for this job execution
        _currentUserId = jobRequest.UserId ?? "System";

        try
        {
            // Extract location type and IDs using typed properties
            var locationType = jobRequest.GetLocationType();
            var locationIds = jobRequest.GetLocationIds();
            
            // Initial counts
            var primaryCount = locationIds.Count;
            var hierarchicalCount = 0;
            var actualTotalLocations = primaryCount;

            _logger.LogInformation(
                "Starting connectivity assessment job {JobId} for {LocationCount} {LocationType}s. " +
                "Hierarchical: {Hierarchical}, Depth: {Depth}, Force: {Force}, " +
                "ActualTotal: {ActualTotal} (Primary: {Primary}, Hierarchical: {HierarchicalCount}), User: {UserId}",
                jobId, primaryCount, locationType,
                jobRequest.GenerateHierarchicalAssessments,
                jobRequest.HierarchicalDepth, jobRequest.ForceRecalculate,
                actualTotalLocations, primaryCount, hierarchicalCount, _currentUserId);

            // Cache the job request for later use in hierarchical processing
            var jobRequestCacheKey = $"job_request_{jobId}";
            _memoryCache.Set(jobRequestCacheKey, jobRequest, TimeSpan.FromHours(2));

            // PRE-COMPUTE wireless coverage for ALL barangays in ONE operation (massive performance gain)
            // This prevents running wireless computation 100+ times (once per batch)
            Dictionary<Guid, string>? globalWirelessData = null;
            List<(Guid Id, LocationTypeEnum Type, string Name)> allBarangayLocations = new();
            
            // Collect all barangay locations (primary + hierarchical)
            if (locationType == LocationTypeEnum.Barangay)
            {
                allBarangayLocations.AddRange(locationIds.Select(id => (id, locationType, "Primary")));
            }

            // Process hierarchical assessments if enabled
            List<LocationConnectivityDto> hierarchicalResults = new();
            int hierarchicalSkipped = 0;
            List<(Guid Id, LocationTypeEnum Type, string Name)> childLocations = new();
            
            if (jobRequest.GenerateHierarchicalAssessments)
            {
                _logger.LogInformation("Job {JobId}: Hierarchical processing is enabled. Discovering child locations...", jobId);

                childLocations = await GetChildLocationsAsync(
                    locationIds,
                    locationType,
                    jobRequest.HierarchicalDepth,
                    cancellationToken);

                hierarchicalCount = childLocations.Count;
                actualTotalLocations = primaryCount + hierarchicalCount;
                
                // Add barangay child locations to wireless computation list
                allBarangayLocations.AddRange(
                    childLocations.Where(l => l.Type == LocationTypeEnum.Barangay)
                    .Select(l => (l.Id, l.Type, l.Name)));
            }
            
            // CRITICAL PERFORMANCE OPTIMIZATION: Compute wireless coverage for ALL barangays at once
            // This runs ONCE instead of 100+ times (once per batch), reducing execution time by 90%+
            if (allBarangayLocations.Count > 0)
            {
                try
                {
                    _logger.LogInformation(
                        "Job {JobId}: Pre-computing wireless coverage for ALL {Count} barangays in ONE operation (major performance optimization)...",
                        jobId, allBarangayLocations.Count);
                    
                    var wirelessGlobalStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    
                    // Load all barangay boundaries
                    var barangayIds = allBarangayLocations.Select(l => l.Id).Distinct().ToList();
                    var barangayProjections = await _barangayRepository.Query()
                        .Where(b => barangayIds.Contains(b.Id) && b.Boundaries != null)
                        .Select(b => new Services.BarangayProjection
                        {
                            Id = b.Id,
                            PSGCCode = b.Code,
                            Boundaries = b.Boundaries
                        })
                        .ToListAsync(cancellationToken);
                    
                    // Create envelope of all barangays for efficient bulk query
                    Geometry? globalBoundary = null;
                    if (barangayProjections.Any())
                    {
                        var factory = barangayProjections.First().Boundaries!.Factory;
                        var combinedEnvelope = new NetTopologySuite.Geometries.Envelope();
                        foreach (var proj in barangayProjections.Where(p => p.Boundaries != null))
                        {
                            combinedEnvelope.ExpandToInclude(proj.Boundaries!.EnvelopeInternal);
                        }
                        globalBoundary = factory.ToGeometry(combinedEnvelope);
                    }
                    
                    // ONE wireless computation for ALL barangays
                    globalWirelessData = await _wirelessCoverageService.ComputeBatchWirelessCoverageAsync(
                        barangayProjections, globalBoundary, cancellationToken);
                    
                    wirelessGlobalStopwatch.Stop();
                    _logger.LogInformation(
                        "Job {JobId}: Pre-computed wireless coverage for {Count} barangays in {ElapsedSeconds:F1}s (avg {AvgMs}ms/barangay). " +
                        "This ran ONCE instead of {BatchCount}+ times - massive performance gain!",
                        jobId, globalWirelessData.Count, wirelessGlobalStopwatch.Elapsed.TotalSeconds,
                        globalWirelessData.Count > 0 ? wirelessGlobalStopwatch.ElapsedMilliseconds / (double)globalWirelessData.Count : 0,
                        (int)Math.Ceiling(allBarangayLocations.Count / (double)BARANGAY_BATCH_SIZE));
                }
                catch (Exception wirelessEx)
                {
                    _logger.LogWarning(wirelessEx,
                        "Job {JobId}: Failed to pre-compute global wireless coverage. " +
                        "Assessments will be created without wireless data (can be added later).",
                        jobId);
                    globalWirelessData = null; // Continue without wireless data
                }
            }
            
            // Process hierarchical locations with pre-computed wireless data
            if (jobRequest.GenerateHierarchicalAssessments && hierarchicalCount > 0)
            {
                var hierarchicalProcessingResult = await ProcessLocationsSequentially(
                    childLocations.Select(l => (l.Id, l.Type, l.Name)).ToList(),
                    jobRequest.ForceRecalculate,
                    jobId,
                    "hierarchical locations",
                    jobRequest.StartDate,
                    jobRequest.EndDate,
                    globalWirelessData, // Pass pre-computed wireless data
                    cancellationToken);
                    
                hierarchicalResults = hierarchicalProcessingResult.Results;
                hierarchicalSkipped = hierarchicalProcessingResult.SkippedCount;
            }

            // Update job status to Processing with startTime and actual total count
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing,
                $"Starting assessment generation for {actualTotalLocations} locations (Primary: {primaryCount}, Hierarchical: {hierarchicalCount})",
                startTime: startTime, processingCount: 0, processedCount: 0, totalCount: actualTotalLocations,
                cancellationToken: cancellationToken);

            var results = new List<LocationConnectivityDto>();

            // Process primary locations with pre-computed wireless data
            var primaryProcessingResult = await ProcessLocationsSequentially(
                locationIds.Select(id => (id, locationType, "Primary")).ToList(),
                jobRequest.ForceRecalculate,
                jobId,
                "primary locations",
                jobRequest.StartDate,
                jobRequest.EndDate,
                globalWirelessData, // Pass pre-computed wireless data
                cancellationToken);

            var primaryResults = primaryProcessingResult.Results;
            var primarySkipped = primaryProcessingResult.SkippedCount;

            results.AddRange(primaryResults);
            results.AddRange(hierarchicalResults);

            // Update progress after primary locations
            var progressMessage = jobRequest.GenerateHierarchicalAssessments
                ? $"Completed {primaryResults.Count}/{primaryCount} primary assessments. Starting hierarchical processing for {hierarchicalCount} locations..."
                : $"Completed {primaryResults.Count}/{primaryCount} primary assessments.";

            var progressPercentage = results.Count * 100.0 / actualTotalLocations;
            _logger.LogInformation(
                "Job {JobId}: Processing completed. Processed: {Processed}/{Expected}, Total progress: {Progress}%",
                jobId, results.Count, actualTotalLocations, progressPercentage.ToString("F1"));

            // Update status with accurate progress tracking
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing,
                progressMessage, processingCount: results.Count, processedCount: results.Count, totalCount: actualTotalLocations,
                cancellationToken: cancellationToken);

            // Clean up cached job request
            _memoryCache.Remove(jobRequestCacheKey);

            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;
            var totalSuccessful = results.Count; // Only count newly saved assessments
            var completionRate = actualTotalLocations > 0 ? (totalSuccessful * 100.0 / actualTotalLocations) : 100;
            var successMessage = $"Successfully generated {totalSuccessful}/{actualTotalLocations} connectivity assessments ({completionRate:F1}%) in {totalTime:mm':'ss}";

            // Calculate actual missing using real skipped counts from processing
            var totalSkipped = primarySkipped + hierarchicalSkipped;
            var actualMissing = actualTotalLocations - totalSuccessful - totalSkipped;

            // Log detailed completion stats with verification
            _logger.LogInformation(
                "Job {JobId} completed. Expected: {Expected}, Saved: {Saved}, Skipped: {Skipped}, Actual Missing: {Missing}, Rate: {Rate}%, " +
                "Primary: {Primary}, Hierarchical: {Hierarchical}, Duration: {Duration}",
                jobId, actualTotalLocations, totalSuccessful, totalSkipped, actualMissing, completionRate,
                primaryCount, hierarchicalCount, totalTime);

            // Warn only if there are actual failures (not just skipped recent assessments)
            if (actualMissing > 0)
            {
                _logger.LogWarning(
                    "Job {JobId} completed with {MissingCount} actual missing assessments out of {Expected} expected. " +
                    "This may be due to locations without boundaries or processing errors. " +
                    "(Note: {SkippedCount} recent assessments were intentionally skipped)",
                    jobId, actualMissing, actualTotalLocations, totalSkipped);
            }

            // CRITICAL: Verify database state before marking as complete
            _logger.LogInformation("Job {JobId}: Performing final database verification before marking as complete...", jobId);
            try
            {
                // Verify ALL assessments created (primary + hierarchical), grouped by location type
                var dbVerificationCount = 0;
                var locationsByType = results.Where(r => r != null)
                    .GroupBy(r => r.LocationType)
                    .ToList();
                
                foreach (var typeGroup in locationsByType)
                {
                    var groupLocationIds = typeGroup.Select(r => r.LocationId).ToList();
                    var typeCount = await VerifyDatabasePersistence(groupLocationIds, typeGroup.Key, cancellationToken);
                    dbVerificationCount += typeCount;
                    _logger.LogInformation("Job {JobId}: Verified {Count} {LocationType} assessments in database",
                        jobId, typeCount, typeGroup.Key);
                }
                
                _logger.LogInformation("Job {JobId}: Database verification confirmed {DbCount} total assessments persisted out of {ExpectedCount} processed",
                    jobId, dbVerificationCount, totalSuccessful);
                
                if (dbVerificationCount < totalSuccessful)
                {
                    var dbDiscrepancy = totalSuccessful - dbVerificationCount;
                    _logger.LogError(
                        "Job {JobId}: DATABASE DISCREPANCY DETECTED - Processed {ProcessedCount} but only {DbCount} found in database. " +
                        "Missing: {MissingInDb}. This indicates database write failures!",
                        jobId, totalSuccessful, dbVerificationCount, dbDiscrepancy);
                    
                    // Update success message to reflect database reality
                    successMessage = $"Generated {dbVerificationCount}/{actualTotalLocations} connectivity assessments ({(actualTotalLocations > 0 ? (dbVerificationCount * 100.0 / actualTotalLocations) : 0):F1}%) in {totalTime:mm':'ss}. " +
                        $"WARNING: {dbDiscrepancy} assessments may not have been saved to database.";
                }
                else if (dbVerificationCount == 0 && totalSuccessful > 0)
                {
                    _logger.LogError(
                        "Job {JobId}: CRITICAL - Database verification returned 0 but {ProcessedCount} were supposedly saved. " +
                        "This likely indicates the verification query timed out or failed.",
                        jobId, totalSuccessful);
                    
                    successMessage = $"Generated {totalSuccessful}/{actualTotalLocations} connectivity assessments ({completionRate:F1}%) in {totalTime:mm':'ss}. " +
                        $"WARNING: Database verification failed - actual persist count unknown.";
                }
            }
            catch (OperationCanceledException verifyEx)
            {
                _logger.LogWarning(verifyEx, 
                    "Job {JobId}: Database verification was canceled (likely timeout). " +
                    "Job completing with {ProcessedCount} assessments processed, but verification incomplete.",
                    jobId, totalSuccessful);
                
                successMessage = $"Generated {totalSuccessful}/{actualTotalLocations} connectivity assessments ({completionRate:F1}%) in {totalTime:mm':'ss}. " +
                    $"WARNING: Database verification canceled - actual persist count unverified.";
            }
            catch (Exception verifyEx)
            {
                _logger.LogError(verifyEx, 
                    "Job {JobId}: Failed to verify database state. Job completing with uncertainty about data persistence.",
                    jobId);
                
                successMessage = $"Generated {totalSuccessful}/{actualTotalLocations} connectivity assessments ({completionRate:F1}%) in {totalTime:mm':'ss}. " +
                    $"WARNING: Database verification failed - actual persist count unknown.";
            }

            // Update job status to Completed
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Completed, successMessage,
                endTime: endTime, processingCount: totalSuccessful, processedCount: totalSuccessful, totalCount: actualTotalLocations,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Job {JobId} completed successfully. {Message}", jobId, successMessage);
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var errorMessage = $"Error in job {jobId}: {ex.Message}";
            _logger.LogError(ex, "Connectivity assessment job {JobId} failed", jobId);

            // Clean up cached job request on error
            var jobRequestCacheKey = $"job_request_" + jobId;
            _memoryCache.Remove(jobRequestCacheKey);

            // Update job status to Failed with endTime
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Failed, errorMessage,
                endTime: endTime, processingCount: 0, processedCount: 0, cancellationToken: cancellationToken);
            throw; // Re-throw for Hangfire retry logic
        }
    }

    /// <summary>
    /// Processes locations sequentially with optimized batching and real-time progress tracking.
    /// 
    /// BATCH PROCESSING:
    /// - Dynamic batch sizes based on location type (1 for Regional, up to 50 for Barangay)
    /// - Accumulates changes in memory before saving entire batch in one transaction
    /// - Tracks both optimistic (processing) and pessimistic (saved) counts
    /// 
    /// ERROR HANDLING:
    /// - Individual location failures don't stop batch processing
    /// - Failed locations are tracked and reported
    /// - Batch save failures are logged with detailed error information
    /// 
    /// RETURN VALUE:
    /// - Tuple of (Results, SkippedCount) where Results are successfully saved assessments
    ///   and SkippedCount is the number of locations skipped due to recent assessments
    /// </summary>
    private async Task<(List<LocationConnectivityDto> Results, int SkippedCount)> ProcessLocationsSequentially(
        List<(Guid Id, LocationTypeEnum Type, string Name)> locations,
        bool forceRecalculate,
        string jobId,
        string processingContext,
        DateTime? startDate,
        DateTime? endDate,
        Dictionary<Guid, string>? preComputedWirelessData,
        CancellationToken cancellationToken)
    {
        if (locations.Count == 0)
            return (new List<LocationConnectivityDto>(), 0);

        var locationType = locations.First().Type;
        var batchSize = GetBatchSizeForLocationType(locationType);

        _logger.LogInformation(
            "Job {JobId}: Processing {LocationCount} {Context} with AGGRESSIVE batch size {BatchSize}",
            jobId, locations.Count, processingContext, batchSize);

        var results = new List<LocationConnectivityDto>();
        var failedLocations = new List<(Guid Id, string Name, string Error)>();
        var skippedLocations = new List<(Guid Id, string Name, string Reason)>();
        var startTime = DateTime.UtcNow;
        var processingCount = 0;
        var savedCount = 0;

        var existingAssessments = await PreloadExistingAssessments(locations, forceRecalculate, jobId, cancellationToken);

        var totalBatches = (int)Math.Ceiling((double)locations.Count / batchSize);
        
        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Job {JobId}: Cancellation requested at batch {BatchNumber}/{TotalBatches}",
                    jobId, batchIndex + 1, totalBatches);
                break;
            }

            var batch = locations.Skip(batchIndex * batchSize).Take(batchSize).ToList();
            var batchAssessments = new List<(ConnectivityAssessment Assessment, List<ConnectivityAssessmentProvider> Providers, string LocationName, Guid LocationId)>();
            
            // Process each location in batch
            foreach (var location in batch)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var result = await ProcessSingleLocationForBatch(
                        location.Id, location.Type, location.Name,
                        forceRecalculate, existingAssessments,
                        startDate, endDate, preComputedWirelessData, cancellationToken);
                        
                    if (result != null)
                    {
                        batchAssessments.Add((result.Value.Assessment, result.Value.Providers, 
                            result.Value.LocationName, location.Id));
                    }
                    else
                    {
                        skippedLocations.Add((location.Id, location.Name, "Recent assessment exists"));
                        _logger.LogDebug("Job {JobId}: Skipped {LocationId} - recent assessment exists", 
                            jobId, location.Id);
                    }
                        
                    processingCount++;
                        
                    if (processingCount % PROGRESS_REPORT_INTERVAL == 0)
                    {
                        await ReportProgressWithCumulativeCount(jobId, processingCount, savedCount, 
                            processingContext, startTime, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Job {JobId}: Processing canceled for {LocationId}", 
                        jobId, location.Id);
                    failedLocations.Add((location.Id, location.Name, "Operation canceled"));
                    processingCount++;
                    break;
                }
                catch (Exception ex)
                {
                    failedLocations.Add((location.Id, location.Name, ex.Message));
                    _logger.LogError(ex, "Job {JobId}: Error processing {LocationId}: {Error}",
                        jobId, location.Id, ex.Message);
                    processingCount++;
                }
                
                stopwatch.Stop();
                _logger.LogDebug(
                    "Job {JobId}: Processed {LocationId} in {ElapsedMilliseconds}ms",
                    jobId, location.Id, stopwatch.ElapsedMilliseconds);
            }
                
            // CRITICAL: Save batch with immediate verification
            if (batchAssessments.Count > 0)
            {
                var batchLocationIds = batchAssessments.Select(b => b.LocationId).ToList();
                var batchLocationType = batchAssessments.First().Assessment.LocationType;
                    
                try
                {
                    var totalCells = batchAssessments.Sum(b => b.Assessment.CellTowerCount);
                    var totalNAPs = batchAssessments.Sum(b => b.Assessment.FiberNAPCount);
                    var totalWiFi = batchAssessments.Sum(b => b.Assessment.WifiHotspotCount);

                    _logger.LogInformation(
                        "Job {JobId}: Batch {BatchNum}/{TotalBatches} - Saving {Count} assessments. " +
                        "Assets: Cells={Cells}, NAPs={NAPs}, WiFi={WiFi}",
                        jobId, batchIndex + 1, totalBatches, batchAssessments.Count,
                        totalCells, totalNAPs, totalWiFi);

                    // Save with aggressive timeout
                    using var saveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    saveCts.CancelAfter(TimeSpan.FromSeconds(BATCH_SAVE_TIMEOUT_SECONDS));

                    var saveStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    await _connectivityRepository.SaveChangesAsync(saveCts.Token);
                    saveStopwatch.Stop();

                    _logger.LogInformation(
                        "Job {JobId}: Batch save completed in {Elapsed}ms",
                        jobId, saveStopwatch.ElapsedMilliseconds);

                    // MANDATORY: Verify immediately after save
                    var verifyStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var verifiedCount = await VerifyBatchPersistence(
                        batchLocationIds, batchLocationType, cancellationToken);
                    verifyStopwatch.Stop();

                    _logger.LogInformation(
                        "Job {JobId}: Verification completed in {Elapsed}ms - {Found}/{Expected} confirmed",
                        jobId, verifyStopwatch.ElapsedMilliseconds, verifiedCount, batchAssessments.Count);

                    // Check if verification matches expected count
                    if (verifiedCount != batchAssessments.Count)
                    {
                        var missing = batchAssessments.Count - verifiedCount;
                        _logger.LogError(
                            "Job {JobId}: BATCH VERIFICATION FAILED - Expected {Expected} but only " +
                            "{Found} in database. Missing: {Missing}. RETRYING...",
                            jobId, batchAssessments.Count, verifiedCount, missing);

                        // ONE RETRY with longer timeout
                        await Task.Delay(2000, cancellationToken);
                            
                        using var retryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        retryCts.CancelAfter(TimeSpan.FromSeconds(BATCH_SAVE_TIMEOUT_SECONDS * 2));
                            
                        await _connectivityRepository.SaveChangesAsync(retryCts.Token);
                        _logger.LogInformation("Job {JobId}: Retry save completed", jobId);
                            
                        verifiedCount = await VerifyBatchPersistence(
                            batchLocationIds, batchLocationType, cancellationToken);
                            
                        if (verifiedCount != batchAssessments.Count)
                        {
                            _logger.LogError(
                                "Job {JobId}: RETRY FAILED - Still only {Found}/{Expected} in database",
                                jobId, verifiedCount, batchAssessments.Count);
                                
                            foreach (var (_, _, locationName, locationId) in batchAssessments)
                            {
                                if (!failedLocations.Any(f => f.Id == locationId))
                                {
                                    failedLocations.Add((locationId, locationName, 
                                        $"Database write failed verification: {verifiedCount}/{batchAssessments.Count}"));
                                }
                            }
                            continue;
                        }
                    }

                    // SUCCESS: Convert verified assessments to DTOs
                    foreach (var (assessment, providers, locationName, locationId) in batchAssessments)
                    {
                        var dto = CreateLocationConnectivityDto(assessment, locationName);
                        results.Add(dto);
                        savedCount++;
                    }

                    _logger.LogInformation(
                        "Job {JobId}: Batch {BatchNum}/{TotalBatches} SUCCESS - " +
                        "{Count} assessments saved and verified",
                        jobId, batchIndex + 1, totalBatches, verifiedCount);

                    await ReportProgressWithCumulativeCount(jobId, processingCount, savedCount,
                        processingContext, startTime, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError(
                        "Job {JobId}: Batch save TIMEOUT after {Timeout}s for {Count} assessments",
                        jobId, BATCH_SAVE_TIMEOUT_SECONDS, batchAssessments.Count);

                    foreach (var (_, _, locationName, locationId) in batchAssessments)
                    {
                        if (!failedLocations.Any(f => f.Id == locationId))
                        {
                            failedLocations.Add((locationId, locationName, 
                                $"Batch save timeout after {BATCH_SAVE_TIMEOUT_SECONDS}s"));
                        }
                    }
                    continue;
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx,
                        "Job {JobId}: CRITICAL - Batch save EXCEPTION for {Count} assessments: {Error}",
                        jobId, batchAssessments.Count, saveEx.Message);

                    foreach (var (_, _, locationName, locationId) in batchAssessments)
                    {
                        if (!failedLocations.Any(f => f.Id == locationId))
                        {
                            failedLocations.Add((locationId, locationName, 
                                $"Save exception: {saveEx.Message}"));
                        }
                    }
                }
            }

            // Memory management
            if (batchIndex % MEMORY_CHECK_INTERVAL == 0 && batchIndex > 0)
            {
                await PerformMemoryManagement(jobId, batchIndex);
            }

            if (batchIndex < totalBatches - 1)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        var totalElapsed = DateTime.UtcNow - startTime;
        
        _logger.LogInformation(
            "Job {JobId}: Completed {Context}. Saved={Saved}, Skipped={Skipped}, Failed={Failed}. " +
            "Duration: {Duration:mm\\:ss}, Rate: {Rate:F1}/sec",
            jobId, processingContext, results.Count, skippedLocations.Count, failedLocations.Count,
            totalElapsed, totalElapsed.TotalSeconds > 0 ? results.Count / totalElapsed.TotalSeconds : 0);

        return (results, skippedLocations.Count);
    }

    /// <summary>
    /// Determines optimal batch size based on location type and processing complexity.
    /// 
    /// BATCH SIZES:
    /// - Regional: 1 (each region has 1K-4K child locations, large spatial queries)
    /// - Provincial: 5 (each province has 100-300 child locations)
    /// - Municipality: 25 (each municipality has 10-50 barangays)
    /// - Barangay: 50 (leaf level, no child locations, fastest processing)
    /// 
    /// RATIONALE:
    /// Smaller batches for hierarchical locations balance transaction size with processing efficiency.
    /// </summary>
    private int GetBatchSizeForLocationType(LocationTypeEnum locationType)
    {
        return locationType switch
        {
            LocationTypeEnum.Regional => REGIONAL_BATCH_SIZE,
            LocationTypeEnum.Provincial => PROVINCIAL_BATCH_SIZE,
            LocationTypeEnum.Municipality => MUNICIPALITY_BATCH_SIZE,
            LocationTypeEnum.Barangay => BARANGAY_BATCH_SIZE,
            _ => BARANGAY_BATCH_SIZE // Default to barangay batch size for unknown types
        };
    }

    private async Task<int> VerifyBatchPersistence(
    List<Guid> locationIds,
    LocationTypeEnum locationType,
    CancellationToken cancellationToken)
{
    try
    {
        using var verifyCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        verifyCts.CancelAfter(TimeSpan.FromMinutes(VERIFICATION_TIMEOUT_MINUTES));

        var dbAssessments = await _connectivityRepository.GetByLocationIdsAsync(
            locationIds, locationType, verifyCts.Token);

        var count = dbAssessments?.Count ?? 0;
        _logger.LogDebug("Batch verification: {Found}/{Expected} confirmed", count, locationIds.Count);
        return count;
    }
    catch (OperationCanceledException)
    {
        _logger.LogError("CRITICAL: Batch verification TIMED OUT after {Timeout}s for {Count} locations",
            VERIFICATION_TIMEOUT_MINUTES, locationIds.Count);
        return 0;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "CRITICAL: Batch verification FAILED for {Count} locations", locationIds.Count);
        return 0;
    }
}

    /// <summary>
    /// Pre-loads existing assessments to optimize database access and reduce query overhead.
    /// 
    /// OPTIMIZATION:
    /// - Loads all assessments for a batch in one query
    /// - Caches results in dictionary for O(1) lookup
    /// - Skipped when force recalculate is enabled
    /// 
    /// RETURN VALUE:
    /// Dictionary keyed by (LocationId, LocationType) tuple for fast lookup during batch processing.
    /// </summary>
    private async Task<Dictionary<(Guid, LocationTypeEnum), ConnectivityAssessment>> PreloadExistingAssessments(
        List<(Guid Id, LocationTypeEnum Type, string Name)> locations,
        bool forceRecalculate,
        string jobId,
        CancellationToken cancellationToken)
    {
        var existingAssessments = new Dictionary<(Guid, LocationTypeEnum), ConnectivityAssessment>();

        if (forceRecalculate)
        {
            _logger.LogDebug("Job {JobId}: Skipping pre-load (force recalculate enabled)", jobId);
            return existingAssessments;
        }

        _logger.LogInformation("Job {JobId}: Pre-loading existing assessments for {LocationCount} locations", jobId, locations.Count);

        var locationIds = locations.Select(l => l.Id).Distinct().ToList();
        var locationType = locations.First().Type; // Assuming all locations are same type in batch

        try
        {
            var existing = await _connectivityRepository.GetByLocationIdsAsync(locationIds, locationType, cancellationToken);

            foreach (var assessment in existing)
            {
                existingAssessments[(assessment.LocationId, assessment.LocationType)] = assessment;
            }

            _logger.LogInformation("Job {JobId}: Pre-loaded {ExistingCount} existing assessments", jobId, existingAssessments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Job {JobId}: Error pre-loading assessments, will check individually", jobId);
        }

        return existingAssessments;
    }



    /// <summary>
    /// Processes a single location and prepares assessment data for batch saving.
    /// 
    /// DEFERRED PERSISTENCE:
    /// - Generates or updates assessment in memory only
    /// - Does not call SaveChangesAsync (handled by batch processing)
    /// - Returns null if location should be skipped (recent assessment exists)
    /// 
    /// RETURN VALUE:
    /// - Tuple of (Assessment, Providers, LocationName) for successful processing
    /// - Null if assessment is recent and force recalculate is false
    /// </summary>
    private async Task<(ConnectivityAssessment Assessment, List<ConnectivityAssessmentProvider> Providers, string LocationName)?> ProcessSingleLocationForBatch(
        Guid locationId,
        LocationTypeEnum locationType,
        string locationName,
        bool forceRecalculate,
        Dictionary<(Guid, LocationTypeEnum), ConnectivityAssessment> existingAssessments,
        DateTime? startDate,
        DateTime? endDate,
        Dictionary<Guid, string>? wirelessCoverageData,
        CancellationToken cancellationToken)
    {
        try
        {
            ConnectivityAssessment? existing = null;

            // First check the pre-loaded cache
            if (existingAssessments.TryGetValue((locationId, locationType), out existing))
            {
                // If not forcing recalculation and assessment is recent, skip regeneration
                if (!forceRecalculate && existing.LastUpdated > DateTime.UtcNow.AddDays(-RECENT_ASSESSMENT_DAYS))
                {
                    _logger.LogDebug("Skipping {LocationType} {LocationId} - recent assessment exists", locationType, locationId);
                    // Return null to indicate no processing needed (assessment is recent)
                    return null;
                }
            }
            else
            {
                // If not in cache, check database directly to avoid duplicate key errors
                existing = await _connectivityRepository.GetByLocationIdAsync(locationId, locationType, cancellationToken);

                // If found and not forcing recalculation and is recent, skip regeneration
                if (existing != null && !forceRecalculate && existing.LastUpdated > DateTime.UtcNow.AddDays(-RECENT_ASSESSMENT_DAYS))
                {
                    _logger.LogDebug("Skipping {LocationType} {LocationId} - recent assessment exists (from DB)", locationType, locationId);
                    // Return null to indicate no processing needed (assessment is recent)
                    return null;
                }
            }

            // Generate new assessment
            _logger.LogDebug("Generating new assessment for {LocationType} {LocationId} (existing: {HasExisting}, force: {ForceRecalculate})",
               locationType, locationId, existing != null, forceRecalculate);

            var newAssessmentData = await GenerateNewAssessment(locationId, locationType, forceRecalculate, startDate, endDate, wirelessCoverageData, cancellationToken);

            // Get location details to retrieve boundary for infrastructure calculation
            var (_, boundary, center, areaKm2, _, _) = await GetLocationDetailsAsync(locationId, locationType, cancellationToken);

            // Calculate infrastructure stats to get provider breakdown
            var infrastructureStats = await CalculateInfrastructureStatsAsync(
                boundary,
                center ?? new Point(0, 0),
                areaKm2,
                locationId,
                locationType,
                forceRecalculate,
                startDate,
                endDate,
                cancellationToken);

            // Log calculated infrastructure stats
            _logger.LogInformation(
                "Calculated infrastructure for {LocationType} {LocationId} ({LocationName}): " +
                "Cells={Cells}, NAPs={NAPs}, WiFi={WiFi}, Providers={Providers}, DominantProvider={Dominant}",
                locationType, locationId, locationName,
                infrastructureStats.CellTowerCount,
                infrastructureStats.FiberNAPCount,
                infrastructureStats.WiFiHotspotCount,
                infrastructureStats.ServiceProviderCount,
                infrastructureStats.DominantServiceProvider ?? "None");

            if (existing != null)
            {
                // Update existing assessment
                UpdateExistingAssessment(existing, newAssessmentData);
                _connectivityRepository.Update(existing);

                // Update provider breakdown - delete old records and insert new ones
                await _connectivityRepository.DeleteProvidersByAssessmentIdAsync(existing.Id, cancellationToken);

                // Prepare provider data for batch insert
                var providers = infrastructureStats.ProviderBreakdown
                    .Select(provider => new ConnectivityAssessmentProvider
                    {
                        ConnectivityAssessmentId = existing.Id,
                        ProviderName = provider.ProviderName,
                        CellTowerCount = provider.CellTowerCount,
                        FiberNAPCount = provider.FiberNAPCount,
                        WiFiHotspotCount = provider.WiFiHotspotCount,
                        TotalAssetCount = provider.TotalAssetCount,
                        AssetPercentage = provider.AssetPercentage
                    })
                    .ToList();

                await _connectivityRepository.AddProvidersRangeAsync(providers, cancellationToken);

                _logger.LogInformation(
                    "Prepared UPDATED assessment for {LocationType} {LocationId}: " +
                    "Cells={Cells}, NAPs={NAPs}, WiFi={WiFi}, Providers={ProviderCount}, " +
                    "ProviderBreakdown: {ProviderDetails}",
                    locationType, locationId,
                    existing.CellTowerCount, existing.FiberNAPCount, existing.WifiHotspotCount,
                    providers.Count,
                    string.Join(", ", providers.Select(p => $"{p.ProviderName}(C:{p.CellTowerCount},N:{p.FiberNAPCount},W:{p.WiFiHotspotCount})")));

                return (existing, providers, locationName);
            }
            else
            {
                // Create new assessment (but don't save yet)
                await _connectivityRepository.AddAsync(newAssessmentData, cancellationToken);
        
                // Prepare provider breakdown for batch insert
                var providers = infrastructureStats.ProviderBreakdown
                    .Select(provider => new ConnectivityAssessmentProvider
                    {
                        ConnectivityAssessmentId = newAssessmentData.Id,
                        ProviderName = provider.ProviderName,
                        CellTowerCount = provider.CellTowerCount,
                        FiberNAPCount = provider.FiberNAPCount,
                        WiFiHotspotCount = provider.WiFiHotspotCount,
                        TotalAssetCount = provider.TotalAssetCount,
                        AssetPercentage = provider.AssetPercentage
                    })
                    .ToList();

                await _connectivityRepository.AddProvidersRangeAsync(providers, cancellationToken);

                _logger.LogInformation(
                    "Prepared NEW assessment for {LocationType} {LocationId}: " +
                    "Cells={Cells}, NAPs={NAPs}, WiFi={WiFi}, Providers={ProviderCount}, " +
                    "ProviderBreakdown: {ProviderDetails}",
                    locationType, locationId,
                    newAssessmentData.CellTowerCount, newAssessmentData.FiberNAPCount, newAssessmentData.WifiHotspotCount,
                    providers.Count,
                    string.Join(", ", providers.Select(p => $"{p.ProviderName}(C:{p.CellTowerCount},N:{p.FiberNAPCount},W:{p.WiFiHotspotCount})")));

                return (newAssessmentData, providers, locationName);
            }
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully without full error stack
            _logger.LogWarning("Processing canceled for {LocationType} {LocationId} ({LocationName}). " +
                "Job may be timing out or shutting down. This is expected during cancellation.",
                locationType, locationId, locationName);
            
            // Re-throw to propagate cancellation up the call stack
            throw;
        }
        catch (Exception ex)
        {
            // Log comprehensive error details including inner exceptions
            var errorDetails = new System.Text.StringBuilder();
            errorDetails.AppendLine($"Error processing assessment for {locationType} {locationId}");
            errorDetails.AppendLine($"Exception Type: {ex.GetType().FullName}");
            errorDetails.AppendLine($"Message: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                errorDetails.AppendLine($"Inner Exception: {ex.InnerException.GetType().FullName}");
                errorDetails.AppendLine($"Inner Message: {ex.InnerException.Message}");
                
                if (ex.InnerException.InnerException != null)
                {
                    errorDetails.AppendLine($"Inner Inner Exception: {ex.InnerException.InnerException.GetType().FullName}");
                    errorDetails.AppendLine($"Inner Inner Message: {ex.InnerException.InnerException.Message}");
                }
            }
            
            _logger.LogError(ex, "DETAILED ERROR - {ErrorDetails}", errorDetails.ToString());
            _logger.LogError(ex, "Error processing assessment for {LocationType} {LocationId}. This location will be skipped.", 
                locationType, locationId);
            
            // Return null for this specific location, but log the error so we can track failures
            // The calling method will continue processing other locations
            return null;
        }
    }

    /// <summary>
    /// Generates a complete connectivity assessment with full CMTS rubric implementation.
    /// 
    /// ASSESSMENT COMPONENTS:
    /// - Location details: Name, boundary, center, area, population, households
    /// - Infrastructure stats: Actual counts of cells, NAPs, WiFi (using optimized queries)
    /// - Speed test stats: Average/median speeds, latency, signal strength
    /// - Technology breakdown: 5G, 4G LTE, 3G, 2G counts and percentages
    /// - CMTS scores: 5 components totaling 0-15 points
    /// - Coverage calculations: Population-aware percentages
    /// 
    /// CMTS SCORING:
    /// 1. Fiber Coverage (0-3 points)
    /// 2. Internet Access (0-3 points)
    /// 3. Technology Type (0-3 points)
    /// 4. Quality of Service (0-3 points)
    /// 5. Coverage Availability (0-3 points)
    /// </summary>
    private async Task<ConnectivityAssessment> GenerateNewAssessment(
        Guid locationId,
        LocationTypeEnum locationType,
        bool forceRecalculate,
        DateTime? startDate,
        DateTime? endDate,
        Dictionary<Guid, string>? wirelessCoverageData,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get location information and boundaries
            var (locationName, boundary, center, areaKm2, population, householdCount) = await GetLocationDetailsAsync(locationId, locationType, cancellationToken);
            if (center == null)
            {
                _logger.LogWarning("Could not get location information for location {LocationId}", locationId);
                // Return basic assessment for unknown locations
                return new()
                {
                    LocationId = locationId,
                    LocationType = locationType,
                    AssessmentDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    ConnectivityStatus = ConnectivityStatusEnum.Unserved,
                    TotalScore = 0,
                    FiberCoverageScore = 0,
                    InternetAccessScore = 0,
                    TechnologyTypeScore = 0,
                    QualityOfServiceScore = 0,
                    CoverageAvailabilityScore = 0,
                    AssessedBy = _currentUserId
                };
            }

            // Calculate infrastructure statistics within the boundary (with caching)
            var infrastructureStats = await CalculateInfrastructureStatsAsync(boundary, center, areaKm2, locationId, locationType, forceRecalculate, startDate, endDate, cancellationToken);

            // Calculate speed test statistics (with caching)
            var speedStats = await CalculateSpeedTestStatsAsync(boundary, center, areaKm2, locationId, locationType, forceRecalculate, startDate, endDate, cancellationToken);

            // Get technology breakdown with caching
            var techCacheKey = $"tech:{locationType}:{locationId}";
            TechnologyBreakdownDto technologyBreakdown;
            if (!forceRecalculate && _memoryCache.TryGetValue(techCacheKey, out TechnologyBreakdownDto? cachedTech) && cachedTech != null)
            {
                technologyBreakdown = cachedTech;
            }
            else
            {
                if (forceRecalculate)
                {
                    _logger.LogInformation("Force recalculating technology breakdown for {LocationType} {LocationId}", locationType, locationId);
                }
                technologyBreakdown = await AnalyzeTechnologyGenerationsAsync(locationId, locationType, boundary, center, areaKm2, true, startDate, endDate, cancellationToken);
                _memoryCache.Set(techCacheKey, technologyBreakdown, TimeSpan.FromHours(12));
            }

            // Calculate CMTS scores based on infrastructure, speed data, and population
            var cmtsScores = CalculateCMTSScores(infrastructureStats, speedStats, areaKm2, population, householdCount);

            // Determine connectivity status and total score
            var totalScore = cmtsScores.FiberCoverage + cmtsScores.InternetAccess +
                            cmtsScores.TechnologyType + cmtsScores.QualityOfService + cmtsScores.CoverageAvailability;

            var connectivityStatus = DetermineConnectivityStatus(totalScore);
            var hasReliableConnectivity = DetermineReliableConnectivity(infrastructureStats, speedStats);

            // Calculate coverage statistics to be saved
            var coverage = CalculatePopulationAwareCoverage(infrastructureStats, areaKm2, population, householdCount, hasReliableConnectivity);

            // OPTIMIZED: Use pre-computed wireless coverage data for barangays (from batch computation)
            string? wirelessCoverageJson = null;
            DateTime? wirelessCoverageLastUpdated = null;
            
            if (locationType == LocationTypeEnum.Barangay)
            {
                // Try to use pre-computed batch data first
                if (wirelessCoverageData != null && wirelessCoverageData.TryGetValue(locationId, out var preComputedJson))
                {
                    // Use pre-computed wireless coverage from batch operation
                    wirelessCoverageJson = preComputedJson;
                    wirelessCoverageLastUpdated = DateTime.UtcNow;
                    _logger.LogDebug("Using pre-computed wireless coverage for barangay {BarangayId}", locationId);
                }
                else
                {
                    // FALLBACK: Compute individually if batch data is not available
                    // This ensures wireless data is always computed, especially during forced recalculation
                    _logger.LogInformation(
                        "Batch wireless data not available for barangay {BarangayId}. Computing individually... (ForceRecalc: {Force})",
                        locationId, forceRecalculate);
                    
                    try
                    {
                        wirelessCoverageJson = await _wirelessCoverageService.ComputeBarangayWirelessCoverageAsync(
                            locationId, boundary, center, cancellationToken);
                        wirelessCoverageLastUpdated = wirelessCoverageJson != null ? DateTime.UtcNow : (DateTime?)null;
                        
                        if (wirelessCoverageJson != null)
                        {
                            _logger.LogInformation(
                                "Successfully computed individual wireless coverage for barangay {BarangayId} (size: {Size} bytes)",
                                locationId, wirelessCoverageJson.Length);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Wireless coverage computation returned null for barangay {BarangayId}. Check if boundary is valid.",
                                locationId);
                        }
                    }
                    catch (Exception wirelessEx)
                    {
                        _logger.LogError(wirelessEx,
                            "Failed to compute individual wireless coverage for barangay {BarangayId}. Wireless data will be null.",
                            locationId);
                        wirelessCoverageJson = null;
                        wirelessCoverageLastUpdated = null;
                    }
                }
            }

            return new()
            {
                LocationId = locationId,
                LocationType = locationType,
                Location = center,
                AreaSizeKm2 = areaKm2,
                Population = population,
                HouseholdCount = householdCount,
                AssessmentDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                AssessedBy = _currentUserId,
                ConnectivityStatus = connectivityStatus,
                TotalScore = totalScore,
                FiberCoverageScore = cmtsScores.FiberCoverage,
                InternetAccessScore = cmtsScores.InternetAccess,
                TechnologyTypeScore = cmtsScores.TechnologyType,
                QualityOfServiceScore = cmtsScores.QualityOfService,
                CoverageAvailabilityScore = cmtsScores.CoverageAvailability,
                CellTowerCount = infrastructureStats.CellTowerCount,
                FiberNAPCount = infrastructureStats.FiberNAPCount,
                WifiHotspotCount = infrastructureStats.WiFiHotspotCount,
                ServiceProviderCount = infrastructureStats.ServiceProviderCount,
                DominantServiceProvider = infrastructureStats.DominantServiceProvider,
                AverageDownloadSpeedMbps = speedStats?.AverageDownloadSpeedMbps,
                AverageUploadSpeedMbps = speedStats?.AverageUploadSpeedMbps,
                AverageLatencyMs = speedStats?.AverageLatencyMs,
                MedianDownloadSpeedMbps = speedStats?.MedianDownloadSpeedMbps,
                MedianUploadSpeedMbps = speedStats?.MedianUploadSpeedMbps,
                SpeedTestCount = speedStats?.SpeedTestCount ?? 0,
                AverageSignalStrength = speedStats?.AverageSignalStrength,
                MeetsMinimumSpeedRequirement = speedStats?.MeetsMinimumSpeedRequirement ?? false,
                HasReliableConnectivity = coverage.HasReliableConnectivity,
                MobileCoveragePercentage = coverage.MobileCoveragePercentage,
                FixedBroadbandCoveragePercentage = coverage.FixedBroadbandCoveragePercentage,
                FiberCoveragePercentage = coverage.FiberCoveragePercentage,
                EstimatedPopulationCoveragePercent = coverage.EstimatedPopulationCoveragePercent,
                EstimatedHouseholdCoveragePercent = coverage.EstimatedHouseholdCoveragePercent,
                AssetsPerThousandPeople = coverage.AssetsPerThousandPeople,
                ProvidersPerThousandPeople = coverage.ProvidersPerThousandPeople,
                WirelessCoverageJson = wirelessCoverageJson,
                WirelessCoverageLastUpdated = wirelessCoverageLastUpdated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating connectivity assessment for {LocationType} {LocationId}", locationType, locationId);
            throw;
        }
    }

    /// <summary>
    /// Get location details including boundaries and population data
    /// Updated to always load boundaries for government-grade accuracy
    /// </summary>
    private async Task<(string Name, Geometry? Boundary, Point? Center, double AreaKm2, int? Population, int? HouseholdCount)> GetLocationDetailsAsync(
        Guid locationId, LocationTypeEnum locationType, CancellationToken cancellationToken)
    {
        return locationType switch
        {
            LocationTypeEnum.Barangay => await GetBarangayDetailsOptimizedAsync(locationId, cancellationToken),
            LocationTypeEnum.Municipality => await GetMunicipalityDetailsOptimizedAsync(locationId, cancellationToken),
            LocationTypeEnum.Provincial => await GetProvinceDetailsOptimizedAsync(locationId, cancellationToken),
            LocationTypeEnum.Regional => await GetRegionDetailsOptimizedAsync(locationId, cancellationToken),
            _ => ("Unknown", null, null, 0, null, null)
        };
    }

    /// <summary>
    /// Optimized barangay details retrieval - always loads boundaries for government-grade accuracy
    /// </summary>
    private async Task<(string Name, Geometry? Boundary, Point? Center, double AreaKm2, int? Population, int? HouseholdCount)> GetBarangayDetailsOptimizedAsync(
        Guid barangayId, CancellationToken cancellationToken)
    {
        var barangayInfo = await _barangayRepository.FirstOrDefaultAsync(
            _barangayRepository.QueryAsNoTracking(b => b.Id == barangayId)
                .Select(b => new { b.Name, b.Center, b.Area, b.TotalPopulation, b.HouseholdCount }),
            cancellationToken);

        if (barangayInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Always load boundaries for barangays - they're typically small and accuracy is important
        Geometry? boundary = null;
        var boundaryQuery = _barangayRepository.QueryAsNoTracking(b => b.Id == barangayId)
            .Select(b => b.Boundaries);
        boundary = await _barangayRepository.FirstOrDefaultAsync(boundaryQuery, cancellationToken);

        return (barangayInfo.Name, boundary, barangayInfo.Center, barangayInfo.Area,
               barangayInfo.TotalPopulation, barangayInfo.HouseholdCount);
    }

    /// <summary>
    /// Optimized municipality details - always loads boundaries for government-grade accuracy
    /// </summary>
    private async Task<(string Name, Geometry? Boundary, Point? Center, double AreaKm2, int? Population, int? HouseholdCount)> GetMunicipalityDetailsOptimizedAsync(
        Guid municipalityId, CancellationToken cancellationToken)
    {
        var municipalityInfo = await _municipalityRepository.FirstOrDefaultAsync(
            _municipalityRepository.QueryAsNoTracking(m => m.Id == municipalityId)
                .Select(m => new { m.Name, m.Center, m.Area, m.TotalPopulation, m.HouseholdCount }),
            cancellationToken);

        if (municipalityInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Always load boundaries for accurate government mapping - performance optimized with caching
        Geometry? boundary = null;
        var boundaryQuery = _municipalityRepository.QueryAsNoTracking(m => m.Id == municipalityId)
            .Select(m => m.Boundaries);
        boundary = await _municipalityRepository.FirstOrDefaultAsync(boundaryQuery, cancellationToken);

        return (municipalityInfo.Name, boundary, municipalityInfo.Center, municipalityInfo.Area,
               municipalityInfo.TotalPopulation, municipalityInfo.HouseholdCount);
    }

    /// <summary>
    /// Province details - always loads boundaries for government-grade accuracy
    /// </summary>
    private async Task<(string Name, Geometry? Boundary, Point? Center, double AreaKm2, int? Population, int? HouseholdCount)> GetProvinceDetailsOptimizedAsync(
        Guid provinceId, CancellationToken cancellationToken)
    {
        var provinceInfo = await _provinceRepository.FirstOrDefaultAsync(
            _provinceRepository.QueryAsNoTracking(p => p.Id == provinceId)
                .Select(p => new { p.Name, p.Center, p.Area, p.TotalPopulation, p.HouseholdCount }),
            cancellationToken);

        if (provinceInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Always load boundaries for accurate government mapping - performance optimized with caching
        Geometry? boundary = null;
        var boundaryQuery = _provinceRepository.QueryAsNoTracking(p => p.Id == provinceId)
            .Select(p => p.Boundaries);
        boundary = await _provinceRepository.FirstOrDefaultAsync(boundaryQuery, cancellationToken);

        return (provinceInfo.Name, boundary, provinceInfo.Center, provinceInfo.Area,
               provinceInfo.TotalPopulation, provinceInfo.HouseholdCount);
    }

    /// <summary>
    /// Region details - loads boundaries for maximum accuracy (performance managed via caching)
    /// </summary>
    private async Task<(string Name, Geometry? Boundary, Point? Center, double AreaKm2, int? Population, int? HouseholdCount)> GetRegionDetailsOptimizedAsync(
        Guid regionId, CancellationToken cancellationToken)
    {
        var regionInfo = await _regionRepository.FirstOrDefaultAsync(
            _regionRepository.QueryAsNoTracking(r => r.Id == regionId)
                .Select(r => new { r.Name, r.Center, r.Area, r.TotalPopulation, r.HouseholdCount }),
            cancellationToken);

        if (regionInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Load boundaries for accurate government mapping - large regions benefit most from caching
        Geometry? boundary = null;
        var boundaryQuery = _regionRepository.QueryAsNoTracking(r => r.Id == regionId)
            .Select(r => r.Boundaries);
        boundary = await _regionRepository.FirstOrDefaultAsync(boundaryQuery, cancellationToken);

        return (regionInfo.Name, boundary, regionInfo.Center, regionInfo.Area,
               regionInfo.TotalPopulation, regionInfo.HouseholdCount);
    }

    /// <summary>
    /// Calculates infrastructure statistics with caching and delegates to optimized boundary calculation.
    /// 
    /// CACHING STRATEGY:
    /// - Cache key: "infra:{locationType}:{locationId}"
    /// - Cache duration: 24 hours
    /// - Bypassed when forceRecalculate is true
    /// 
    /// This method serves as a caching wrapper around CalculateInfrastructureWithBoundaryOptimization.
    /// </summary>
    private async Task<InfrastructureStatsDto> CalculateInfrastructureStatsAsync(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        // Only check cache if not forcing recalculation
        if (!forceRecalculate)
        {
            var cacheKey = $"infra:{locationType}:{locationId}";
            if (_memoryCache.TryGetValue(cacheKey, out InfrastructureStatsDto? cachedResult) && cachedResult != null)
            {
                _logger.LogDebug("Using cached infrastructure stats for {LocationType} {LocationId}", locationType, locationId);
                return cachedResult;
            }
        }
        else
        {
            _logger.LogInformation("Force recalculating infrastructure stats for {LocationType} {LocationId} (ignored cache)", locationType, locationId);
        }

        _logger.LogDebug("Calculating infrastructure stats for {LocationType} {LocationId} (Area: {Area} km�)", locationType, locationId, areaKm2);

        // Use accurate boundaries with optimizations
        var result = await CalculateInfrastructureWithBoundaryOptimization(boundary, center, areaKm2, locationId, locationType, forceRecalculate, startDate, endDate, cancellationToken);

        // Always cache the fresh result
        var infraCacheKey = $"infra:{locationType}:{locationId}";
        _memoryCache.Set(infraCacheKey, result, TimeSpan.FromHours(24));

        return result;
    }

    /// <summary>
    /// Calculates speed test statistics with optimized queries and caching.
    /// 
    /// QUERY OPTIMIZATION:
    /// - Regional/Provincial: Subquery-based filtering (no boundary materialization)
    /// - Municipal/Barangay: Envelope filtering (single boundary materialization)
    /// 
    /// CACHING:
    /// - Cache key: "speed:{locationType}:{locationId}"
    /// - Cache duration: 12 hours (more volatile than infrastructure)
    /// - Bypassed when forceRecalculate is true
    /// 
    /// CALCULATIONS:
    /// - Average and median speeds (download/upload)
    /// - Average latency and signal strength
    /// - Minimum speed requirement check (25 Mbps download)
    /// </summary>
    private async Task<SpeedStatsDto?> CalculateSpeedTestStatsAsync(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        // Only check cache if not forcing recalculation
        if (!forceRecalculate)
        {
            var cacheKey = $"speed:{locationType}:{locationId}";
            if (_memoryCache.TryGetValue(cacheKey, out SpeedStatsDto? cachedResult) && cachedResult != null)
            {
                _logger.LogDebug("Using cached speed stats for {LocationType} {LocationId}", locationType, locationId);
                return cachedResult;
            }
        }
        else
        {
            _logger.LogInformation("Force recalculating speed stats for {LocationType} {LocationId} (ignored cache)", locationType, locationId);
        }

        SpeedStatsDto? result = null;

        if (boundary != null)
        {
            var query = _speedtestRepository.QueryAsNoTracking();
            
            // Use conditional filtering: subquery for large areas (prevents timeout), envelope for small areas (faster than correlated subquery)
            if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
            {
                _logger.LogInformation("{LocationType} {LocationId}: Using subquery-based filtering (large area - NO boundary materialization)", locationType, locationId);
                query = locationType == LocationTypeEnum.Regional
                    ? await ApplyLocationBoundaryFilterAsync(query, regionId: locationId, cancellationToken: cancellationToken)
                    : await ApplyLocationBoundaryFilterAsync(query, provinceId: locationId, cancellationToken: cancellationToken);
            }
            else
            {
                // For small areas (Municipality/Barangay), envelope filtering with materialized boundary is faster
                _logger.LogDebug("{LocationType} {LocationId}: Using envelope filter (small area - single boundary materialization)", locationType, locationId);
                query = GeoQueryHelper.ApplyEnvelopeFilter(boundary, query);
            }

            // Apply date range filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.CreatedAt <= endOfDay);
            }

            try
            {
                // OPTIMIZATION: Use database-level aggregation instead of loading all records into memory
                // This prevents PostgreSQL shared memory exhaustion when processing large regions
                var count = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .CountAsync(query, cancellationToken);
                
                _logger.LogInformation("Processing {Count} speedtest records for location {LocationId} ({LocationType})", 
                    count, locationId, locationType);

                if (count == 0)
                {
                    result = new SpeedStatsDto
                    {
                        SpeedTestCount = 0,
                        AverageDownloadSpeedMbps = 0,
                        AverageUploadSpeedMbps = 0,
                        MedianDownloadSpeedMbps = 0,
                        MedianUploadSpeedMbps = 0,
                        AverageLatencyMs = 0,
                        AverageSignalStrength = 0,
                        MeetsMinimumSpeedRequirement = false
                    };
                }
                else if (count > 100000)
                {
                    // For very large datasets, use pure SQL aggregation to avoid memory issues
                    _logger.LogWarning("Large speedtest dataset ({Count} records) - using database aggregation for location {LocationId}", 
                        count, locationId);
                    
                    result = await CalculateSpeedTestStatsWithDatabaseAggregation(query, count, cancellationToken);
                }
                else
                {
                    // For smaller datasets, load into memory for median calculation
                    var speedtestResults = await _speedtestRepository.ToListAsync(
                        query.Select(s => new SpeedtestDto
                        {
                            DownloadSpeed = s.DownloadSpeed,
                            UploadSpeed = s.UploadSpeed,
                            Ping = s.Ping,
                            RSSI = s.RSSI,
                            SignalQuality = s.SignalQuality
                        }),
                        cancellationToken);

                    result = ProcessSpeedTestResults(speedtestResults);
                }
            }
            catch (Exception speedTestEx)
            {
                _logger.LogError(speedTestEx, "Error querying speedtests for location {LocationId} ({LocationType}). Error: {ErrorMessage}",
                    locationId, locationType, speedTestEx.Message);
                throw;
            }
        }
        else
        {
            // Radius-based fallback
            var searchRadiusKm = Math.Sqrt(areaKm2 / Math.PI);
            var searchRadiusMeters = searchRadiusKm * 1000;

            var query = _speedtestRepository.QueryAsNoTracking()
                .Where(s => s.Location.Distance(center) <= searchRadiusMeters);

            // Apply date range filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.CreatedAt <= endOfDay);
            }

            try
            {
                // OPTIMIZATION: Use database-level aggregation for large datasets
                var count = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .CountAsync(query, cancellationToken);
                
                _logger.LogInformation("Processing {Count} speedtest records (radius-based) for location {LocationId} ({LocationType})", 
                    count, locationId, locationType);

                if (count == 0)
                {
                    result = new SpeedStatsDto
                    {
                        SpeedTestCount = 0,
                        AverageDownloadSpeedMbps = 0,
                        AverageUploadSpeedMbps = 0,
                        MedianDownloadSpeedMbps = 0,
                        MedianUploadSpeedMbps = 0,
                        AverageLatencyMs = 0,
                        AverageSignalStrength = 0,
                        MeetsMinimumSpeedRequirement = false
                    };
                }
                else if (count > 100000)
                {
                    // For very large datasets, use pure SQL aggregation to avoid memory issues
                    _logger.LogWarning("Large speedtest dataset ({Count} records) - using database aggregation for location {LocationId}", 
                        count, locationId);
                    
                    result = await CalculateSpeedTestStatsWithDatabaseAggregation(query, count, cancellationToken);
                }
                else
                {
                    // For smaller datasets, load into memory for median calculation
                    var speedtestResults = await _speedtestRepository.ToListAsync(
                        query.Select(s => new SpeedtestDto
                        {
                            DownloadSpeed = s.DownloadSpeed,
                            UploadSpeed = s.UploadSpeed,
                            Ping = s.Ping,
                            RSSI = s.RSSI,
                            SignalQuality = s.SignalQuality
                        }),
                        cancellationToken);

                    result = ProcessSpeedTestResults(speedtestResults);
                }
            }
            catch (Exception speedTestEx)
            {
                _logger.LogError(speedTestEx, "Error querying speedtests (radius) for location {LocationId} ({LocationType}). Error: {ErrorMessage}",
                    locationId, locationType, speedTestEx.Message);
                throw;
            }
        }

        // Always cache the fresh result
        if (result != null)
        {
            var speedCacheKey = $"speed:{locationType}:{locationId}";
            _memoryCache.Set(speedCacheKey, result, TimeSpan.FromHours(4));
        }

        return result;
    }

    /// <summary>
    /// Calculate speed test stats using database-level aggregation to avoid loading large datasets into memory.
    /// This prevents PostgreSQL shared memory exhaustion (Error 53100) for regions with millions of speedtest records.
    /// For large datasets, we use database aggregation for averages and approximate median using percentile approach.
    /// </summary>
    private async Task<SpeedStatsDto> CalculateSpeedTestStatsWithDatabaseAggregation(
        IQueryable<Speedtest> query, 
        int totalCount, 
        CancellationToken cancellationToken)
    {
        // Calculate averages at database level using Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        var avgDownload = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .AverageAsync(query.Select(s => s.DownloadSpeed), cancellationToken);
        var avgUpload = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .AverageAsync(query.Select(s => s.UploadSpeed), cancellationToken);
        
        var pingQuery = query.Where(s => s.Ping > 0).Select(s => s.Ping);
        var hasPing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(pingQuery, cancellationToken);
        var avgPing = hasPing 
            ? await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AverageAsync(pingQuery, cancellationToken)
            : 0;
        
        // For RSSI, filter out null values first - calculate signal strength score
        var rssiQuery = query.Where(s => s.RSSI.HasValue).Select(s => s.RSSI!.Value);
        var hasRssi = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(rssiQuery, cancellationToken);
        double avgSignalStrength = 0;
        
        if (hasRssi)
        {
            var avgRssi = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AverageAsync(rssiQuery, cancellationToken);
            // Convert RSSI to signal strength score (1-5)
            avgSignalStrength = avgRssi switch
            {
                <= -100 => 1.0, // No coverage
                <= -90 => 2.0,  // Very weak
                <= -80 => 3.0,  // Weak
                <= -70 => 4.0,  // Average
                _ => 5.0        // Good
            };
        }

        // For median approximation, skip to middle record (faster than loading all records)
        // Note: This is an approximation but avoids loading massive datasets into memory
        var medianPosition = totalCount / 2;
        var medianDownload = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                query.OrderBy(s => s.DownloadSpeed)
                    .Skip(medianPosition)
                    .Select(s => s.DownloadSpeed), 
                cancellationToken);
            
        var medianUpload = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                query.OrderBy(s => s.UploadSpeed)
                    .Skip(medianPosition)
                    .Select(s => s.UploadSpeed), 
                cancellationToken);

        var avgDownloadDecimal = (decimal)avgDownload;
        return new SpeedStatsDto
        {
            SpeedTestCount = totalCount,
            AverageDownloadSpeedMbps = avgDownloadDecimal,
            AverageUploadSpeedMbps = (decimal)avgUpload,
            MedianDownloadSpeedMbps = medianDownload,
            MedianUploadSpeedMbps = medianUpload,
            AverageLatencyMs = (decimal)avgPing,
            AverageSignalStrength = avgSignalStrength,
            MeetsMinimumSpeedRequirement = avgDownloadDecimal >= 25
        };
    }

    /// <summary>
    /// Process speed test results and calculate statistics
    /// </summary>
    private static SpeedStatsDto? ProcessSpeedTestResults(List<SpeedtestDto> speedtests)
    {
        if (speedtests.Count == 0)
            return null;

        var downloadSpeeds = speedtests
            .Select(s => s.DownloadSpeed)
            .OrderBy(s => s).ToList();

        var uploadSpeeds = speedtests
            .Select(s => s.UploadSpeed)
            .OrderBy(s => s).ToList();

        // Calculate signal quality metrics from RSSI data
        var rssiValues = speedtests
            .Where(s => s.RSSI.HasValue)
            .Select(s => s.RSSI!.Value)
            .ToList();

        var signalQualities = speedtests
            .Where(s => !string.IsNullOrEmpty(s.SignalQuality))
            .Select(s => s.SignalQuality!)
            .ToList();

        // Determine average signal strength using SignalStrengthHelper
        var averageSignalStrength = SignalStrengthHelper.CalculateAverageSignalStrength(rssiValues, signalQualities);

        return new()
        {
            AverageDownloadSpeedMbps = Math.Round(downloadSpeeds.Average(), 2),
            AverageUploadSpeedMbps = Math.Round(uploadSpeeds.Average(), 2),
            AverageLatencyMs = Math.Round(speedtests.Average(s => s.Ping), 2),
            MedianDownloadSpeedMbps = CalculateMedian(downloadSpeeds),
            MedianUploadSpeedMbps = CalculateMedian(uploadSpeeds),
            SpeedTestCount = speedtests.Count,
            MeetsMinimumSpeedRequirement = downloadSpeeds.Average() >= 25, // 25 Mbps minimum
            AverageSignalStrength = averageSignalStrength
        };
    }

    /// <summary>
    /// Analyzes technology generations in the area from speedtest data and cellular infrastructure
    /// </summary>
    private async Task<TechnologyBreakdownDto> AnalyzeTechnologyGenerationsAsync(Guid locationId, LocationTypeEnum locationType, Geometry? boundary, Point center, double areaKm2, bool useBoundary, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            var allTechData = new List<(string GenerationName, string TechnologyStandard, Guid Id)>();

            if (useBoundary && boundary != null)
            {
                var speedtestQuery = _speedtestRepository.QueryAsNoTracking();
                
                // Conditional filtering: subquery for large areas, envelope for small areas
                if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
                {
                    speedtestQuery = locationType == LocationTypeEnum.Regional
                        ? await ApplyLocationBoundaryFilterAsync(speedtestQuery, regionId: locationId, cancellationToken: cancellationToken)
                        : await ApplyLocationBoundaryFilterAsync(speedtestQuery, provinceId: locationId, cancellationToken: cancellationToken);
                }
                else
                {
                    speedtestQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, speedtestQuery);
                }

                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    speedtestQuery = speedtestQuery.Where(s => s.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    speedtestQuery = speedtestQuery.Where(s => s.CreatedAt <= endOfDay);
                }

                var speedtestResults = await _speedtestRepository.ToListAsync(
                    speedtestQuery
                        .Where(s => s.Generation != null && s.Technology != null)
                        .Select(s => new
                        {
                            GenerationName = s.Generation!.Name,
                            TechnologyStandard = s.Technology!.Standard,
                            s.Id
                        }), cancellationToken);

                allTechData.AddRange(speedtestResults.Select(s => (s.GenerationName, s.TechnologyStandard, s.Id)));

                // Get cellular technology data using cell repository with spatial filter
                var cellQuery = _cellRepository.QueryAsNoTracking();
                
                // Conditional filtering: subquery for large areas, envelope for small areas
                if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
                {
                    cellQuery = locationType == LocationTypeEnum.Regional
                        ? await ApplyLocationBoundaryFilterAsync(cellQuery, regionId: locationId, cancellationToken: cancellationToken)
                        : await ApplyLocationBoundaryFilterAsync(cellQuery, provinceId: locationId, cancellationToken: cancellationToken);
                }
                else
                {
                    cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);
                }

                var cellularResults = await _cellRepository.ToListAsync(
                    cellQuery
                        .SelectMany(cell => cell.CellSectors!)
                        .SelectMany(sector => sector.Cellulars!)
                        .Where(c => c.Technology != null && c.Technology.Generation != null)
                        .Select(c => new
                        {
                            GenerationName = c.Technology!.Generation!.Name,
                            TechnologyStandard = c.Technology!.Standard,
                            c.Id
                        }), cancellationToken);

                allTechData.AddRange(cellularResults.Select(c => (c.GenerationName, c.TechnologyStandard, c.Id)));

                // Get wireless validation technology data with conditional filtering
                IQueryable<WirelessValidation> wirelessValidationQuery;
                
                if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
                {
                    // Large areas: use barangay ID subquery
                    IQueryable<Guid> barangayIds = locationType == LocationTypeEnum.Regional
                        ? _barangayRepository.QueryAsNoTracking()
                            .Where(b => b.Municipality != null && b.Municipality.Province != null && b.Municipality.Province.RegionId == locationId)
                            .Select(b => b.Id)
                        : _barangayRepository.QueryAsNoTracking()
                            .Where(b => b.Municipality != null && b.Municipality.ProvinceId == locationId)
                            .Select(b => b.Id);
                    
                    wirelessValidationQuery = _wirelessValidationRepository.QueryAsNoTracking()
                        .Where(wv => barangayIds.Contains(wv.BarangayId)
                                   && wv.Technology != null
                                   && wv.Technology.Generation != null);
                }
                else
                {
                    // Small areas: use Within() with materialized boundary
                    wirelessValidationQuery = _wirelessValidationRepository.QueryAsNoTracking()
                        .Where(wv => wv.Barangay != null
                                   && wv.Barangay.Center != null
                                   && wv.Barangay.Center.Within(boundary!)
                                   && wv.Technology != null
                                   && wv.Technology.Generation != null);
                }

                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
                }

                var wirelessValidationResults = await _wirelessValidationRepository.ToListAsync(
                    wirelessValidationQuery.Select(wv => new
                    {
                        GenerationName = wv.Technology!.Generation!.Name,
                        TechnologyStandard = wv.Technology!.Standard,
                        wv.Id
                    }), cancellationToken);

                allTechData.AddRange(wirelessValidationResults.Select(wv => (wv.GenerationName, wv.TechnologyStandard, wv.Id)));
            }
            else
            {
                // Use radius-based queries for larger areas (no cap for technology generations)
                var searchRadiusKm = Math.Sqrt(areaKm2 / Math.PI);
                var searchRadiusMeters = searchRadiusKm * 1000;

                var speedtestQuery = _speedtestRepository.QueryAsNoTracking()
                    .Where(s => s.Location.Distance(center) <= searchRadiusMeters
                              && s.Generation != null && s.Technology != null);

                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    speedtestQuery = speedtestQuery.Where(s => s.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    speedtestQuery = speedtestQuery.Where(s => s.CreatedAt <= endOfDay);
                }

                var speedtestResults = await _speedtestRepository.ToListAsync(
                    speedtestQuery.Select(s => new
                    {
                        GenerationName = s.Generation!.Name,
                        TechnologyStandard = s.Technology!.Standard,
                        s.Id
                    }), cancellationToken);

                allTechData.AddRange(speedtestResults.Select(s => (s.GenerationName, s.TechnologyStandard, s.Id)));

                var cellularResults = await _cellularRepository.ToListAsync(
                    _cellularRepository.QueryAsNoTracking()
                        .Where(c => c.CellSector!.Cell.Location.Distance(center) <= searchRadiusMeters)
                        .Select(c => new
                        {
                            GenerationName = c.Technology!.Generation!.Name,
                            TechnologyStandard = c.Technology!.Standard,
                            c.Id
                        }), cancellationToken);

                allTechData.AddRange(cellularResults.Select(c => (c.GenerationName, c.TechnologyStandard, c.Id)));

                // Get wireless validation technology data using barangay center distance
                var wirelessValidationQuery = _wirelessValidationRepository.QueryAsNoTracking()
                    .Where(wv => wv.Barangay != null
                               && wv.Barangay.Center != null
                               && wv.Barangay.Center.Distance(center) <= searchRadiusMeters
                               && wv.Technology != null
                               && wv.Technology.Generation != null);

                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
                }

                var wirelessValidationResults = await _wirelessValidationRepository.ToListAsync(
                    wirelessValidationQuery.Select(wv => new
                    {
                        GenerationName = wv.Technology!.Generation!.Name,
                        TechnologyStandard = wv.Technology!.Standard,
                        wv.Id
                    }), cancellationToken);

                allTechData.AddRange(wirelessValidationResults.Select(wv => (wv.GenerationName, wv.TechnologyStandard, wv.Id)));
            }

            if (allTechData.Count == 0)
            {
                return new();
            }

            // Analyze generation distribution
            var generationCounts = allTechData
                .GroupBy(t => NormalizeGenerationName(t.GenerationName))
                .ToDictionary(g => g.Key, g => g.Count());

            var technologyStandards = allTechData
                .Select(t => t.TechnologyStandard)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();

            var generationNames = allTechData
                .Select(t => t.GenerationName)
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct()
                .ToList();

            var dominantGeneration = generationCounts.Any()
                ? generationCounts.OrderByDescending(kvp => kvp.Value).First().Key
                : null;

            var count2G = generationCounts.GetValueOrDefault("2G", 0);
            var count3G = generationCounts.GetValueOrDefault("3G", 0);
            var count4G = generationCounts.GetValueOrDefault("4G", 0);
            var count5G = generationCounts.GetValueOrDefault("5G", 0);

            return new()
            {
                Count2G = count2G,
                Count3G = count3G,
                Count4G = count4G,
                Count5G = count5G,
                AvailableTechnologies = technologyStandards,
                AvailableGenerations = generationNames,
                DominantGeneration = dominantGeneration,
                PrimaryTechnology = dominantGeneration ?? "Unknown",
                HasModernCoverage = count4G > 0 || count5G > 0,
                ModernCoverageRatio = allTechData.Count > 0 ? Math.Round((double)(count4G + count5G) / allTechData.Count * 100, 2) : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing technology generations for area. Returning empty breakdown.");
            return new();
        }
    }

    /// <summary>
    /// Normalizes generation names to standard format (2G, 3G, 4G, 5G)
    /// </summary>
    private static string NormalizeGenerationName(string generationName)
    {
        if (string.IsNullOrEmpty(generationName))
            return string.Empty;

        var normalized = generationName.ToUpperInvariant().Trim();

        return normalized switch
        {
            var s when s.Contains("5G") || s.Contains("NR") || s.Contains("FIFTH") || s.Contains("5G NR") => "5G",
            var s when s.Contains("4G") || s.Contains("LTE") || s.Contains("LTE-A") || s.Contains("FOURTH") => "LTE",
            var s when s.Contains("3G") || s.Contains("UMTS") || s.Contains("WCDMA") || s.Contains("HSPA") || s.Contains("THIRD") => "3G",
            var s when s.Contains("2G") || s.Contains("GSM") || s.Contains("EDGE") || s.Contains("GPRS") || s.Contains("SECOND") => "2G",
            _ => string.Empty
        };
    }

    /// Analyzes band frequency distribution from cellular deployments in the area
    /// Uses database-side aggregation with single query and projection
    /// </summary>
    private async Task<BandBreakdownDto> AnalyzeBandFrequencyAsync(Guid locationId, LocationTypeEnum locationType, Geometry? boundary, Point center, double areaKm2, bool useBoundary, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            // Dictionary to accumulate band counts from both sources
            var bandCounts = new Dictionary<string, int>();

            if (useBoundary && boundary != null)
            {
                // Determine if we should use subquery filtering to avoid boundary materialization
                var useSubqueryFilter = locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial;
                
                // OPTIMIZED: Single query with joins - let database do the work
                var cellQuery = _cellRepository.Query();
                
                if (useSubqueryFilter)
                {
                    cellQuery = locationType == LocationTypeEnum.Regional
                        ? await ApplyLocationBoundaryFilterAsync(cellQuery, regionId: locationId, cancellationToken: cancellationToken)
                        : await ApplyLocationBoundaryFilterAsync(cellQuery, provinceId: locationId, cancellationToken: cancellationToken);
                }
                else
                {
                    cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);
                }

                // Get band data from Cellular using single query with joins
                var cellularBandData = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => c.CellSector != null 
                                 && c.CellSector.Cell != null
                                 && cellQuery.Select(cell => cell.Id).Contains(c.CellSector.Cell.Id)
                                 && c.Band != null)
                        .Select(c => c.Band!.Name), cancellationToken);

                // Aggregate cellular band data
                foreach (var normalizedBand in cellularBandData
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Select(b => NormalizeBandName(b))
                    .Where(nb => !string.IsNullOrEmpty(nb)))
                {
                    bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                }

                // Get wireless validation band data with conditional filtering
                IQueryable<WirelessValidation> wirelessValidationQuery;
                
                if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
                {
                    // Large areas: use barangay ID subquery
                    IQueryable<Guid> barangayIds = locationType == LocationTypeEnum.Regional
                        ? _barangayRepository.QueryAsNoTracking()
                            .Where(b => b.Municipality != null && b.Municipality.Province != null && b.Municipality.Province.RegionId == locationId)
                            .Select(b => b.Id)
                        : _barangayRepository.QueryAsNoTracking()
                            .Where(b => b.Municipality != null && b.Municipality.ProvinceId == locationId)
                            .Select(b => b.Id);
                    
                    wirelessValidationQuery = _wirelessValidationRepository.Query()
                        .Where(wv => barangayIds.Contains(wv.BarangayId)
                                   && wv.Band != null);
                }
                else
                {
                    // Small areas: use Within() with materialized boundary
                    wirelessValidationQuery = _wirelessValidationRepository.Query()
                        .Where(wv => wv.Barangay != null
                                   && wv.Barangay.Center != null
                                   && wv.Barangay.Center.Within(boundary!)
                                   && wv.Band != null);
                }
                
                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
                }
                
                var wirelessValidationBandData = await _wirelessValidationRepository.ToListAsync(
                    wirelessValidationQuery.Select(wv => wv.Band!.Name), cancellationToken);

                // Aggregate wireless validation band data
                foreach (var normalizedBand in wirelessValidationBandData
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Select(b => NormalizeBandName(b))
                    .Where(nb => !string.IsNullOrEmpty(nb)))
                {
                    bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                }
            }
            else
            {
                // Use radius-based queries for larger areas (AnalyzeBandFrequencyAsync - line ~1197)
                var searchRadiusKm = Math.Min(Math.Sqrt(areaKm2 / Math.PI), 50);
                var searchRadiusMeters = searchRadiusKm * 1000;

                // OPTIMIZED: Single query with distance filter
                var cellularBandData = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => c.CellSector!.Cell.Location.Distance(center) <= searchRadiusMeters
                                 && c.Band != null)
                        .Select(c => c.Band!.Name), cancellationToken);

                // Aggregate cellular band data
                foreach (var normalizedBand in cellularBandData
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Select(b => NormalizeBandName(b))
                    .Where(nb => !string.IsNullOrEmpty(nb)))
                {
                    bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                }

                // Get wireless validation band data using barangay center distance
                var wirelessValidationQuery = _wirelessValidationRepository.Query()
                    .Where(wv => wv.Barangay != null
                               && wv.Barangay.Center != null
                               && wv.Barangay.Center.Distance(center) <= searchRadiusMeters
                               && wv.Band != null);
                
                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
                }
                
                var wirelessValidationBandData = await _wirelessValidationRepository.ToListAsync(
                    wirelessValidationQuery.Select(wv => wv.Band!.Name), cancellationToken);

                // Aggregate wireless validation band data
                foreach (var normalizedBand in wirelessValidationBandData
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Select(b => NormalizeBandName(b))
                    .Where(nb => !string.IsNullOrEmpty(nb)))
                {
                    bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                }
            }

            if (bandCounts.Count == 0)
            {
                return new BandBreakdownDto();
            }

            // Get unique band names for available bands list
            var availableBands = bandCounts.Keys.OrderBy(b => b).ToList();
            
            // Find dominant band
            var dominantBand = bandCounts.OrderByDescending(kvp => kvp.Value).First().Key;

            return new BandBreakdownDto
            {
                Count700MHz = bandCounts.GetValueOrDefault("700MHz", 0),
                Count850MHz = bandCounts.GetValueOrDefault("850MHz", 0),
                Count900MHz = bandCounts.GetValueOrDefault("900MHz", 0),
                Count1800MHz = bandCounts.GetValueOrDefault("1800MHz", 0),
                Count2100MHz = bandCounts.GetValueOrDefault("2100MHz", 0),
                Count2300MHz = bandCounts.GetValueOrDefault("2300MHz", 0),
                Count2500MHz = bandCounts.GetValueOrDefault("2500MHz", 0),
                Count2600MHz = bandCounts.GetValueOrDefault("2600MHz", 0),
                Count3500MHz = bandCounts.GetValueOrDefault("3500MHz", 0),
                AvailableBands = availableBands,
                DominantBand = dominantBand
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing band frequency for area. Returning empty breakdown.");
            return new BandBreakdownDto();
        }
    }

    /// <summary>
    /// Normalizes band names to standard frequency format (700MHz, 850MHz, etc.)
    /// Handles various band naming conventions including Band numbers and frequency descriptions
    /// </summary>
    private static string NormalizeBandName(string bandName)
    {
        if (string.IsNullOrEmpty(bandName))
            return string.Empty;

        var normalized = bandName.ToUpperInvariant().Trim();

        // Map band numbers and frequencies to standard names
        return normalized switch
        {
            // 700 MHz (Band 28)
            var s when s.Contains("700") || s.Contains("BAND 28") || s.Contains("B28") => "700MHz",
            
            // 850 MHz (Band 5)
            var s when s.Contains("850") || s.Contains("BAND 5") || s.Contains("B5") && !s.Contains("2500") => "850MHz",
            
            // 900 MHz (Band 8)
            var s when s.Contains("900") || s.Contains("BAND 8") || s.Contains("B8") => "900MHz",
            
            // 1800 MHz (Band 3)
            var s when s.Contains("1800") || s.Contains("BAND 3") || s.Contains("B3") => "1800MHz",
            
            // 2100 MHz (Band 1)
            var s when s.Contains("2100") || s.Contains("BAND 1") || s.Contains("B1") && !s.Contains("41") => "2100MHz",
            
            // 2300 MHz (Band 40)
            var s when s.Contains("2300") || s.Contains("BAND 40") || s.Contains("B40") => "2300MHz",
            
            // 2500 MHz (Band 41)
            var s when s.Contains("2500") || s.Contains("BAND 41") || s.Contains("B41") => "2500MHz",
            
            // 2600 MHz (Band 38) - SEPARATED from Band 42
            var s when s.Contains("2600") || s.Contains("BAND 38") || s.Contains("B38") => "2600MHz",
            
            // 3500 MHz (Band 78, Band 42) - Combined as they're all in 3.4-3.8 GHz range
            var s when s.Contains("3500") || s.Contains("3400") || s.Contains("BAND 78") || s.Contains("B78") || 
                       s.Contains("BAND 42") || s.Contains("B42") => "3500MHz",
            
            _ => string.Empty // Unknown band
        };
    }

    /// <summary>
    /// Analyzes technology-specific band availability from cellular deployments
    /// Returns unique band names for each technology standard (e.g., "FDD-LTE", "NR", "HSPAP")
    /// plus generation counts, dominant technology, and modern coverage statistics
    /// OPTIMIZED: Uses database-side aggregation with single query and joins
    /// </summary>
    private async Task<TechnologyBandBreakdownDto> AnalyzeTechnologyBandBreakdownAsync(Guid locationId, LocationTypeEnum locationType, Geometry? boundary, Point center, double areaKm2, bool useBoundary, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            // Dictionary to accumulate unique bands per technology standard
            var techBandSets = new Dictionary<string, HashSet<string>>();
            // Dictionary to track unique bands per generation
            var generationBands = new Dictionary<string, HashSet<string>>();
            // Dictionary to count deployments per technology (for primary technology detection)
            var technologyCounts = new Dictionary<string, int>();

            if (useBoundary && boundary != null)
            {
                var cellQuery = _cellRepository.Query();
                
                // Conditional filtering: subquery for large areas, envelope for small areas
                if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
                {
                    cellQuery = locationType == LocationTypeEnum.Regional
                        ? await ApplyLocationBoundaryFilterAsync(cellQuery, regionId: locationId, cancellationToken: cancellationToken)
                        : await ApplyLocationBoundaryFilterAsync(cellQuery, provinceId: locationId, cancellationToken: cancellationToken);
                }
                else
                {
                    cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);
                }

                // Get technology-band data from Cellular using single query with joins
                var cellularTechBandData = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => c.CellSector != null 
                                 && c.CellSector.Cell != null
                                 && cellQuery.Select(cell => cell.Id).Contains(c.CellSector.Cell.Id)
                                 && c.Band != null
                                 && c.Technology != null
                                 && c.Technology.Standard != null
                                 && c.Technology.Generation != null)
                        .Select(c => new { 
                            BandName = c.Band!.Name, 
                            TechnologyStandard = c.Technology!.Standard,
                            GenerationName = c.Technology!.Generation!.Name
                        }), cancellationToken);

                // Collect unique bands for each technology and count deployments
                foreach (var item in cellularTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    // Track bands per technology
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Count deployments per technology
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    
                    // Track unique bands per generation
                    if (!string.IsNullOrEmpty(item.GenerationName))
                    {
                        if (!generationBands.ContainsKey(item.GenerationName))
                        {
                            generationBands[item.GenerationName] = new HashSet<string>();
                        }
                        generationBands[item.GenerationName].Add(item.BandName);
                    }
                }

                // Get wireless validation technology-band data with conditional filtering
                IQueryable<WirelessValidation> wirelessValidationQuery;
                
                if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
                {
                    // Large areas: use barangay ID subquery
                    IQueryable<Guid> barangayIds = locationType == LocationTypeEnum.Regional
                        ? _barangayRepository.QueryAsNoTracking()
                            .Where(b => b.Municipality != null && b.Municipality.Province != null && b.Municipality.Province.RegionId == locationId)
                            .Select(b => b.Id)
                        : _barangayRepository.QueryAsNoTracking()
                            .Where(b => b.Municipality != null && b.Municipality.ProvinceId == locationId)
                            .Select(b => b.Id);
                    
                    wirelessValidationQuery = _wirelessValidationRepository.Query()
                        .Where(wv => barangayIds.Contains(wv.BarangayId)
                                   && wv.Band != null
                                   && wv.Technology != null
                                   && wv.Technology.Standard != null
                                   && wv.Technology.Generation != null);
                }
                else
                {
                    // Small areas: use Within() with materialized boundary
                    wirelessValidationQuery = _wirelessValidationRepository.Query()
                        .Where(wv => wv.Barangay != null
                                   && wv.Barangay.Center != null
                                   && wv.Barangay.Center.Within(boundary!)
                                   && wv.Band != null
                                   && wv.Technology != null
                                   && wv.Technology.Standard != null
                                   && wv.Technology.Generation != null);
                }
                
                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
                }
                
                var wirelessValidationTechBandData = await _wirelessValidationRepository.ToListAsync(
                    wirelessValidationQuery.Select(wv => new { 
                        BandName = wv.Band!.Name, 
                        TechnologyStandard = wv.Technology!.Standard,
                        GenerationName = wv.Technology!.Generation!.Name
                    }), cancellationToken);

                // Collect unique bands for each technology from wireless validations
                foreach (var item in wirelessValidationTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    // Track bands per technology
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Count deployments
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    
                    // Track unique bands per generation
                    if (!string.IsNullOrEmpty(item.GenerationName))
                    {
                        if (!generationBands.ContainsKey(item.GenerationName))
                        {
                            generationBands[item.GenerationName] = new HashSet<string>();
                        }
                        generationBands[item.GenerationName].Add(item.BandName);
                    }
                }
            }
            else
            {
                // Use radius-based queries for larger areas (AnalyzeTechnologyBandBreakdownAsync - line ~1416)
                var searchRadiusKm = Math.Min(Math.Sqrt(areaKm2 / Math.PI), 50);
                var searchRadiusMeters = searchRadiusKm * 1000;

                // OPTIMIZED: Single query with distance filter
                var cellularTechBandData = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => c.CellSector!.Cell.Location.Distance(center) <= searchRadiusMeters
                                 && c.Band != null
                                 && c.Technology != null
                                 && c.Technology.Standard != null
                                 && c.Technology.Generation != null)
                        .Select(c => new { 
                            BandName = c.Band!.Name, 
                            TechnologyStandard = c.Technology!.Standard,
                            GenerationName = c.Technology!.Generation!.Name
                        }), cancellationToken);

                // Collect unique bands for each technology
                foreach (var item in cellularTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    // Track bands per technology
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Count deployments
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    
                    // Track unique bands per generation
                    if (!string.IsNullOrEmpty(item.GenerationName))
                    {
                        if (!generationBands.ContainsKey(item.GenerationName))
                        {
                            generationBands[item.GenerationName] = new HashSet<string>();
                        }
                        generationBands[item.GenerationName].Add(item.BandName);
                    }
                }

                // Get wireless validation technology-band data using barangay center distance
                var wirelessValidationQuery = _wirelessValidationRepository.Query()
                    .Where(wv => wv.Barangay != null
                               && wv.Barangay.Center != null
                               && wv.Barangay.Center.Distance(center) <= searchRadiusMeters
                               && wv.Band != null
                               && wv.Technology != null
                               && wv.Technology.Standard != null
                               && wv.Technology.Generation != null);
                
                // Apply date range filter if provided
                if (startDate.HasValue)
                {
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
                }
                
                var wirelessValidationTechBandData = await _wirelessValidationRepository.ToListAsync(
                    wirelessValidationQuery.Select(wv => new { 
                        BandName = wv.Band!.Name, 
                        TechnologyStandard = wv.Technology!.Standard,
                        GenerationName = wv.Technology!.Generation!.Name
                    }), cancellationToken);

                // Collect unique bands for each technology from wireless validations
                foreach (var item in wirelessValidationTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    // Track bands per technology
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Count deployments
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    
                    // Track unique bands per generation
                    if (!string.IsNullOrEmpty(item.GenerationName))
                    {
                        if (!generationBands.ContainsKey(item.GenerationName))
                        {
                            generationBands[item.GenerationName] = new HashSet<string>();
                        }
                        generationBands[item.GenerationName].Add(item.BandName);
                    }
                }
            }

            if (!techBandSets.Any() || techBandSets.All(kvp => kvp.Value.Count == 0))
            {
                return new TechnologyBandBreakdownDto();
            }

            // Build the DTO by converting HashSets to sorted Lists for each technology standard
            var result = new TechnologyBandBreakdownDto();
            foreach (var kvp in techBandSets.Where(x => x.Value.Any()))
            {
                result[kvp.Key] = kvp.Value.OrderBy(b => b).ToList();
            }
            
            // Populate generation-based properties using unique band counts
            result.AvailableGenerations = generationBands.Keys.OrderBy(g => g).ToList();
            result.Count2G = generationBands.GetValueOrDefault("2G", new HashSet<string>()).Count;
            result.Count3G = generationBands.GetValueOrDefault("3G", new HashSet<string>()).Count;
            result.Count4G = generationBands.GetValueOrDefault("4G", new HashSet<string>()).Count;
            result.Count5G = generationBands.GetValueOrDefault("5G", new HashSet<string>()).Count;
            
            // Calculate total count
            var totalCount = technologyCounts.Values.Sum();
            result.TotalTechnologyCount = totalCount;
            
            // Determine dominant generation (by unique band count)
            if (generationBands.Any())
            {
                result.DominantGeneration = generationBands.OrderByDescending(kvp => kvp.Value.Count).First().Key;
            }
            
            // Determine primary technology (by deployment count)
            if (technologyCounts.Any())
            {
                result.PrimaryTechnology = technologyCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            }
            
            // Calculate modern coverage statistics
            var modernCount = result.Count4G + result.Count5G;
            result.HasModernCoverage = modernCount > 0;
            result.ModernCoverageRatio = totalCount > 0 ? Math.Round((double)modernCount / totalCount * 100, 2) : 0;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing technology-band breakdown for area. Returning empty breakdown.");
            return new TechnologyBandBreakdownDto();
        }
    }

    /// <summary>
    /// Calculates the median value from an ordered list of decimals.
    /// Handles both even and odd-length lists correctly.
    /// </summary>
    private static decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0) return 0;

        int mid = values.Count / 2;
        return values.Count % 2 == 0
            ? Math.Round((values[mid - 1] + values[mid]) / 2, 2)
            : Math.Round(values[mid], 2);
    }

    /// <summary>
    /// Calculates CMTS (Connectivity Maturity and Technology Score) based on multiple factors.
    /// 
    /// SCORING COMPONENTS (0-3 points each, total 0-15):
    /// 1. Fiber Coverage: Measures fiber infrastructure availability
    /// 2. Internet Access: Measures overall infrastructure availability
    /// 3. Technology Type: Evaluates technology generation mix (5G/4G/3G/2G)
    /// 4. Quality of Service: Based on speed test performance
    /// 5. Coverage Availability: Infrastructure density relative to area and population
    /// 
    /// Each component independently contributes to the total connectivity assessment.
    /// </summary>
    private static CMTSScoresDto CalculateCMTSScores(InfrastructureStatsDto infrastructure, SpeedStatsDto? speedStats, double areaKm2, int? population, int? householdCount)
    {
        var scores = new CMTSScoresDto();

        // Calculate population density context once
        var populationContext = CalculatePopulationContext(population, areaKm2);

        // Calculate each CMTS criterion using dedicated methods
        scores.FiberCoverage = CalculateFiberCoverageScore(infrastructure, areaKm2, population, populationContext);
        scores.InternetAccess = CalculateInternetAccessScore(speedStats);
        scores.TechnologyType = CalculateTechnologyTypeScore(infrastructure);
        scores.QualityOfService = CalculateQualityOfServiceScore(speedStats, infrastructure);
        scores.CoverageAvailability = CalculateCoverageAvailabilityScore(infrastructure, areaKm2, population, populationContext);

        return scores;
    }

    /// <summary>
    /// Calculate population density context for scoring
    /// </summary>
    private static (double PopulationDensity, bool IsUrbanArea, bool IsHighDensityArea) CalculatePopulationContext(int? population, double areaKm2)
    {
        var populationDensity = population.HasValue && areaKm2 > 0 ? population.Value / areaKm2 : 0;
        var isHighDensityArea = populationDensity > 500; // >500 people per km� = high density
        var isUrbanArea = populationDensity > 1000; // >1000 people per km� = urban

        return (populationDensity, isUrbanArea, isHighDensityArea);
    }

    /// <summary>
    /// Calculate Fiber Coverage Score (0-3 points) - CMTS Criterion 1
    /// </summary>
    private static int CalculateFiberCoverageScore(InfrastructureStatsDto infrastructure, double areaKm2, int? population, (double PopulationDensity, bool IsUrbanArea, bool IsHighDensityArea) context)
    {
        // Unserved: No fiber infrastructure at all
        if (infrastructure.FiberNAPCount == 0)
        {
            return 1; // Unserved - No installed or present FTTH facilities
        }

        // Get providers with fiber presence
        var fiberProviders = infrastructure.ProviderBreakdown
            .Where(p => p.FiberNAPCount > 0)
            .ToList();

        if (fiberProviders.Count == 0)
        {
            return 1; // No fiber providers
        }

        // Identify major telco providers (Globe, Smart, DITO)
        var majorTelcoProviders = fiberProviders
            .Where(p => IsMajorTelcoProvider(p.ProviderName))
            .ToList();

        var localVasProviders = fiberProviders
            .Where(p => !IsMajorTelcoProvider(p.ProviderName))
            .ToList();

        // Count distinct major telcos with fiber presence
        var majorTelcoCount = majorTelcoProviders.Count;
        var totalFiberProviderCount = fiberProviders.Count;

        // SERVED (3 points): All major service providers have fiber infrastructure
        // Interpretation: At least 2-3 major telcos present (Globe, Smart, DITO)
        if (majorTelcoCount >= 2)
        {
            return 3; // Served - Fiber facilities for all/most major service providers present
        }

        // UNDERSERVED (2 points): Few major telcos OR only local/VAS providers
        // Case 1: Only 1 major telco with fiber
        if (majorTelcoCount == 1)
        {
            return 2; // Underserved - Few major telcos with fiber
        }

        // Case 2: No major telcos, but local/VAS providers have fiber
        if (majorTelcoCount == 0 && localVasProviders.Any())
        {
            return 2; // Underserved - Only local or VAS providers with fiber
        }

        // Fallback: Has fiber but doesn't fit clear categories
        return 1; // Unserved - Minimal fiber presence
    }

    /// <summary>
    /// Identifies if a provider is a major telecommunications company in the Philippines
    /// Major telcos: Globe, Smart, DITO
    /// </summary>
    private static bool IsMajorTelcoProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return false;

        // Major telco providers in the Philippines
        var majorTelcos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Globe", "Smart", "DITO",
            // Include common variations and subsidiaries
            "Globe Telecom", "Smart Communications", "DITO Telecommunity",
            "PLDT", "Globe Broadband", "Smart Broadband"
        };

        return majorTelcos.Any(telco => providerName.Contains(telco, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Calculate Internet Access Score (0-3 points) - CMTS Criterion 2
    /// Evaluates internet availability, speed, and reliability per CMTS rubric
    /// Uses MEDIAN download speed (more resistant to outliers)
    /// </summary>
    private static int CalculateInternetAccessScore(SpeedStatsDto? speedStats)
    {
        // Unserved: No internet access or only minimal access
        if (speedStats == null || speedStats.SpeedTestCount == 0)
        {
            return 1; // No data - no broadband infrastructure
        }

        // Insufficient data to make reliable assessment
        if (speedStats.SpeedTestCount < 3)
        {
            return 1; // Too few tests to assess reliability
        }

        var avgDownloadSpeed = speedStats.AverageDownloadSpeedMbps ?? 0;
        var medianDownloadSpeed = speedStats.MedianDownloadSpeedMbps ?? 0;

        // Calculate speed consistency (variability indicator)
        // Large difference between average and median indicates unreliability
        var speedVariability = avgDownloadSpeed > 0
            ? Math.Abs((avgDownloadSpeed - medianDownloadSpeed) / avgDownloadSpeed)
            : 0;
        var isUnreliable = speedVariability > 0.3m; // >30% variance = unreliable

        // SERVED (3 points): Stable internet meeting/exceeding national broadband standards
        // Criteria: ≥25 Mbps AND reliable (low variability)
        // Use MEDIAN speed as it's more resistant to outliers
        if (medianDownloadSpeed >= 25 && !isUnreliable)
        {
            return 3; // Served - Stable internet meeting or exceeding standards (25+ Mbps)
        }

        // UNDERSERVED (2 points): Internet available but slow, unreliable, or below standards
        // Case 1: Speed ≥25 Mbps but unreliable
        if (medianDownloadSpeed >= 25 && isUnreliable)
        {
            return 2; // Underserved - Available but unreliable
        }

        // Case 2: Speed between 10-25 Mbps (below standards but functional)
        if (medianDownloadSpeed >= 10 && medianDownloadSpeed < 25)
        {
            return 2; // Underserved - Available but below national broadband standards
        }

        // UNSERVED (1 point): No internet access or only minimal access
        // Speed <10 Mbps = minimal/no practical broadband access
        return 1; // Unserved - No or minimal internet access (<10 Mbps)
    }

    /// <summary>
    /// Calculate Technology Type Score (0-3 points) - CMTS Criterion 3
    /// Evaluates broadband technology infrastructure presence and quality per CMTS rubric
    /// </summary>
    private static int CalculateTechnologyTypeScore(InfrastructureStatsDto infrastructure)
    {
        var hasFiberInfra = infrastructure.FiberNAPCount > 0;
        var hasCellTowerInfra = infrastructure.CellTowerCount > 0;
        var hasWiFiInfra = infrastructure.WiFiHotspotCount > 0;

        // SERVED (3 points): Fiber optic, LTE/5G, or high-speed DSL infrastructure in place and active

        // Case 1: Fiber optic infrastructure present (meets rubric directly)
        if (hasFiberInfra)
        {
            return 3; // Served - Fiber optic infrastructure present
        }

        // Case 2: Modern wireless technology (LTE/5G) active
        // Enhanced technology scoring using actual generation data
        if (infrastructure.TechnologyBandBreakdown != null && infrastructure.TechnologyBandBreakdown.TotalTechnologyCount > 0)
        {
            var tech = infrastructure.TechnologyBandBreakdown;

            // 5G infrastructure present and active
            if (tech.Count5G > 0)
            {
                return 3; // Served - 5G infrastructure in place and active
            }

            // 4G/LTE infrastructure present and dominant (not just occasional)
            if (tech.Count4G > 0 && !tech.IsLegacyDominated)
            {
                return 3; // Served - LTE/4G infrastructure in place and active
            }

            // UNDERSERVED (2 points): Legacy technologies (2G/3G, dial-up, outdated satellite)

            // Has modern tech but legacy dominates
            if (tech.HasModernTechnology && tech.IsLegacyDominated)
            {
                return 2; // Underserved - Mixed modern and legacy, but legacy dominates
            }

            // Only legacy technologies (2G/3G)
            if (tech.Count3G > 0 || tech.Count2G > 0)
            {
                return 2; // Underserved - Only legacy technologies (2G/3G) available
            }

            // No identifiable technology generation
            return 1; // Unserved - No identifiable broadband technology
        }

        // FALLBACK: No technology breakdown data available, use infrastructure presence

        // Has some modern infrastructure (cell towers or WiFi)
        if (hasCellTowerInfra || hasWiFiInfra)
        {
            // Multiple infrastructure types suggest better service
            var infraTypes = (hasCellTowerInfra ? 1 : 0) + (hasWiFiInfra ? 1 : 0);

            if (infraTypes >= 2)
            {
                return 3; // Served - Multiple infrastructure types present
            }

            return 2; // Underserved - Limited infrastructure diversity
        }

        // UNSERVED (1 point): No installed broadband technology
        return 1; // Unserved - No broadband infrastructure detected
    }

    /// <summary>
    /// Calculate Quality of Service Score (0-3 points) - CMTS Criterion 4
    /// Evaluates latency, signal strength, and service reliability per CMTS rubric
    /// </summary>
    private static int CalculateQualityOfServiceScore(SpeedStatsDto? speedStats, InfrastructureStatsDto infrastructure)
    {
        // Insufficient data to assess quality
        if (speedStats == null || speedStats.SpeedTestCount < 3)
        {
            return 1; // Unserved - Cannot assess quality without sufficient data
        }

        var latency = speedStats.AverageLatencyMs ?? decimal.MaxValue;
        var signalStrength = speedStats.AverageSignalStrength;
        var hasConsistentSpeeds = speedStats.MeetsMinimumSpeedRequirement;

        // Map signal strength (1-5 scale) to dBm ranges per rubric
        // 5 = Good (-44 to -70 dBm), 4 = Average (-70 to -85 dBm), 
        // 3 = Weak (-85 to -90 dBm), 2 = Very Weak (-90 to -100 dBm), 
        // 1 = No Coverage (-100+ dBm)

        // UNSERVED (1 point): Poor to nonexistent quality
        // Criteria: Latency >100ms OR signal = 2 (worse than -90 dBm) OR very poor speeds
        if (latency > 100 || signalStrength <= 2.0 || speedStats.AverageDownloadSpeedMbps < 5)
        {
            return 1; // Unserved - Poor quality, unusable for essential tasks
        }

        // SERVED (3 points): High quality service
        // Criteria: Latency <50ms AND good signal (=4 = -44 to -89 dBm)
        var hasLowLatency = latency < 50;
        var hasGoodSignal = signalStrength >= 4.0; // Average to Good signal (-44 to -89 dBm)

        if (hasLowLatency && hasGoodSignal && hasConsistentSpeeds)
        {
            return 3; // Served - High quality, suitable for online learning, remote work, streaming
        }

        // Additional quality check for excellent 5G performance
        if (infrastructure.TechnologyBandBreakdown?.Count5G > 0 &&
            latency <= 20 &&
            speedStats.AverageDownloadSpeedMbps >= 100 &&
            signalStrength >= 4.5)
        {
            return 3; // Served - Exceptional 5G quality
        }

        // UNDERSERVED (2 points): Fair quality with issues
        // Criteria: Latency 50-100ms OR moderate signal (-90 to -99 dBm) OR inconsistent speeds
        // This catches the "middle ground" cases
        var hasModerateLatency = latency >= 50 && latency <= 100;
        var hasModerateSignal = signalStrength >= 2.0 && signalStrength < 4.0; // -90 to -99 dBm range

        if (hasModerateLatency || hasModerateSignal || !hasConsistentSpeeds)
        {
            return 2; // Underserved - Fair quality but suffers from congestion or poor signal
        }

        // Edge case: Has good latency OR good signal but not both
        if (hasLowLatency || hasGoodSignal)
        {
            return 2; // Underserved - Some quality indicators are good but not comprehensive
        }

        // Fallback
        return 1; // Unserved - Quality issues detected
    }

    /// <summary>
    /// Calculate Coverage Availability Score (0-3 points) - CMTS Criterion 5
    /// Based on CMTS Radioplanner Coverage rubric:
    /// - Served (3): 100% of households/population have access to fixed or mobile broadband
    /// - Underserved (2): Only 40% to 99% of the population has access, often clustered in urban centers or specific areas
    /// - Unserved (1): Less than 40% have access; infrastructure (towers, cables, etc.) is sparse or non-existent
    /// </summary>
    private static int CalculateCoverageAvailabilityScore(InfrastructureStatsDto infrastructure, double areaKm2, int? population, (double PopulationDensity, bool IsUrbanArea, bool IsHighDensityArea) context)
    {
        if (infrastructure.TotalAssets == 0)
        {
            return 1; // Unserved - No coverage (no infrastructure)
        }

        double coverageScore;

        if (population.HasValue && population > 0)
        {
            // Population-based coverage calculation (aligns with CMTS rubric)
            var assetsPerCapita = (double)infrastructure.TotalAssets / population.Value;
            var providersPerThousand = (infrastructure.ServiceProviderCount * 1000.0) / population.Value;
            var hasModernTech = infrastructure.TechnologyBandBreakdown?.HasModernTechnology == true;
            var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;

            // Calculate estimated coverage percentage based on infrastructure density
            double estimatedCoveragePercent;

            if (context.IsUrbanArea)
            {
                // Urban: High infrastructure density expected  
                estimatedCoveragePercent = Math.Min(100, (assetsPerCapita * 10000) + (providersPerThousand * 20));
            }
            else if (context.IsHighDensityArea)
            {
                // Semi-urban: Moderate infrastructure density
                estimatedCoveragePercent = Math.Min(100, (assetsPerCapita * 15000) + (providersPerThousand * 25));
            }
            else
            {
                // Rural: Lower per-capita requirements but geographic coverage matters
                var geographicDensity = areaKm2 > 0 ? infrastructure.TotalAssets / areaKm2 : 0;
                var populationCoverage = Math.Min(100, (assetsPerCapita * 20000) + (providersPerThousand * 30));
                var geographicCoverage = Math.Min(100, geographicDensity * 100);
                estimatedCoveragePercent = Math.Max(populationCoverage, geographicCoverage);
            }

            // Apply CMTS rubric thresholds strictly based on coverage percentage
            // SERVED (3 points): 100% of households/population have access
            if (estimatedCoveragePercent >= 100)
            {
                coverageScore = 3; // Served - 100% coverage per CMTS rubric
            }
            // UNDERSERVED (2 points): 40% to 99% of the population has access
            else if (estimatedCoveragePercent >= 40)
            {
                coverageScore = 2; // Underserved - 40-99% coverage per CMTS rubric
            }
            // UNSERVED (1 point): Less than 40% have access; infrastructure is sparse
            else
            {
                coverageScore = 1; // Unserved - <40% coverage per CMTS rubric
            }
        }
        else
        {
            // Fallback to geographic density when population data unavailable
            var assetDensity = areaKm2 > 0 ? infrastructure.TotalAssets / areaKm2 : 0;
            var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;
            var hasModernTech = infrastructure.TechnologyBandBreakdown?.HasModernTechnology == true;

            // Estimate coverage category based on infrastructure presence
            // High density with good provider diversity suggests comprehensive coverage (100%)
            if (hasMultipleProviders && hasModernTech && assetDensity >= 1.0)
            {
                coverageScore = 3; // Served - Comprehensive coverage estimated
            }
            // Moderate infrastructure suggests partial coverage (40-99%)
            else if (hasMultipleProviders || hasModernTech || assetDensity >= 0.5)
            {
                coverageScore = 2; // Underserved - Partial coverage estimated
            }
            // Sparse infrastructure suggests minimal coverage (<40%)
            else
            {
                coverageScore = 1; // Unserved - Limited/sparse coverage estimated
            }
        }

        return (int)Math.Round(coverageScore);
    }

    /// <summary>
    /// Calculates population-aware coverage statistics and ratios.
    /// 
    /// CALCULATED METRICS:
    /// - Assets per thousand people: Infrastructure density relative to population
    /// - Providers per thousand people: Service provider competition metric
    /// - Mobile/Fixed/Fiber coverage percentages: Infrastructure type distribution
    /// - Estimated population/household coverage: Based on infrastructure density
    /// 
    /// DENSITY CLASSIFICATION:
    /// - Urban: >1000 people/km² (requires more infrastructure per capita)
    /// - High density: >500 people/km² (moderate infrastructure requirements)
    /// - Rural: ≤500 people/km² (lower infrastructure requirements)
    /// </summary>
    private static CoverageStatsDto CalculatePopulationAwareCoverage(
        InfrastructureStatsDto infrastructure,
        double areaKm2,
        int? population,
        int? householdCount,
        bool hasReliableConnectivity)
    {
        var coverage = new CoverageStatsDto
        {
            HasReliableConnectivity = hasReliableConnectivity
        };

        // Calculate population-aware metrics if population data is available
        if (population.HasValue && population > 0)
        {
            // Assets per thousand people
            coverage.AssetsPerThousandPeople = (infrastructure.TotalAssets * 1000.0) / population.Value;

            // Providers per thousand people  
            coverage.ProvidersPerThousandPeople = (infrastructure.ServiceProviderCount * 1000.0) / population.Value;

            // Estimate population coverage based on infrastructure density
            var populationDensity = areaKm2 > 0 ? population.Value / areaKm2 : 0;
            var isUrbanArea = populationDensity > 1000;
            var isHighDensityArea = populationDensity > 500;

            double estimatedCoveragePercent;
            var assetsPerCapita = (double)infrastructure.TotalAssets / population.Value;
            var providersPerThousand = coverage.ProvidersPerThousandPeople ?? 0;

            if (isUrbanArea)
            {
                // Urban areas: Higher infrastructure density expected
                estimatedCoveragePercent = Math.Min(100, (assetsPerCapita * 10000) + (providersPerThousand * 20));
            }
            else if (isHighDensityArea)
            {
                // Semi-urban areas: Moderate infrastructure density
                estimatedCoveragePercent = Math.Min(100, (assetsPerCapita * 15000) + (providersPerThousand * 25));
            }
            else
            {
                // Rural areas: Consider both population and geographic coverage
                var geographicDensity = areaKm2 > 0 ? infrastructure.TotalAssets / areaKm2 : 0;
                var populationCoverage = Math.Min(100, (assetsPerCapita * 20000) + (providersPerThousand * 30));
                var geographicCoverage = Math.Min(100, geographicDensity * 100);
                estimatedCoveragePercent = Math.Max(populationCoverage, geographicCoverage);
            }

            coverage.EstimatedPopulationCoveragePercent = Math.Round(estimatedCoveragePercent, 2);

            // Estimate household coverage.
            // Assuming the coverage is evenly distributed, the percentage of covered households 
            // is proportional to the percentage of covered population.
            if (householdCount.HasValue && householdCount > 0)
            {
                // If 100% of the population is covered, 100% of households are covered.
                // Therefore, the percentages are logically equivalent for estimation purposes.
                coverage.EstimatedHouseholdCoveragePercent = Math.Round(estimatedCoveragePercent, 2);
            }
        }

        // Calculate basic coverage percentages based on infrastructure mix
        var totalInfrastructure = infrastructure.TotalAssets;
        if (totalInfrastructure > 0)
        {
            // Mobile coverage based on cell towers
            coverage.MobileCoveragePercentage = Math.Round((double)infrastructure.CellTowerCount / totalInfrastructure * 100, 2);

            // Fixed broadband coverage based on fiber NAPs and WiFi
            var fixedInfrastructure = infrastructure.FiberNAPCount + infrastructure.WiFiHotspotCount;
            coverage.FixedBroadbandCoveragePercentage = Math.Round((double)fixedInfrastructure / totalInfrastructure * 100, 2);

            // Fiber coverage based on NAP presence
            coverage.FiberCoveragePercentage = Math.Round((double)infrastructure.FiberNAPCount / totalInfrastructure * 100, 2);
        }

        return coverage;
    }

    /// <summary>
    /// Updates job status in the database with progress tracking information.
    /// 
    /// TRACKED INFORMATION:
    /// - Status: Processing, Completed, or Failed
    /// - Message: Human-readable status description
    /// - Start/End times: Job execution duration
    /// - Processing count: Locations currently being processed
    /// - Processed count: Locations successfully saved
    /// - Total count: Expected total locations
    /// 
    /// Called at job start, during progress updates, and at completion/failure.
    /// </summary>
    private Task UpdateJobStatus(
        string jobId,
        ConnectivityAssessmentJobStatusEnum status,
        string message,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? processingCount = null,
        int? processedCount = null,
        int? totalCount = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"job_status_{jobId}";
            var currentStatus = _memoryCache.Get<ConnectivityAssessmentJobStatusDto>(cacheKey);

            if (currentStatus == null)
            {
                _logger.LogWarning("Job {JobId} status not found in cache, creating new status entry", jobId);
                currentStatus = new()
                {
                    JobId = jobId,
                    Status = ConnectivityAssessmentJobStatusEnum.Pending,
                    Message = "Job initialized",
                    LastUpdated = DateTime.UtcNow,
                    StartTime = null,
                    EndTime = null,
                    ProcessingCount = 0,
                    ProcessedCount = 0,
                    TotalCount = totalCount ?? 1 // Use provided total or default to 1
                };
            }

            // Update the status with new information
            currentStatus.Status = status;
            currentStatus.Message = message;
            currentStatus.LastUpdated = DateTime.UtcNow;

            if (startTime.HasValue)
                currentStatus.StartTime = startTime.Value;

            if (endTime.HasValue)
                currentStatus.EndTime = endTime.Value;

            if (processingCount.HasValue)
                currentStatus.ProcessingCount = processingCount.Value;

            if (processedCount.HasValue)
                currentStatus.ProcessedCount = processedCount.Value;

            if (totalCount.HasValue)
                currentStatus.TotalCount = totalCount.Value;

            // Update the cache with the new status (extend expiry to 24 hours)
            _memoryCache.Set(cacheKey, currentStatus, TimeSpan.FromHours(24));

            _logger.LogInformation("Job {JobId} status update: {Status} - {Message}. " +
                "StartTime: {StartTime}, EndTime: {EndTime}, ProcessingCount: {ProcessingCount}, ProcessedCount: {ProcessedCount}",
                jobId, status, message, startTime, endTime, processingCount, processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {JobId} status to {Status}", jobId, status);
            // Don't rethrow as this is auxiliary functionality
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reports processing progress with separate optimistic and pessimistic count tracking.
    /// 
    /// COUNT TRACKING:
    /// - Processing count (optimistic): Items that have been processed (may not be saved yet)
    /// - Saved count (pessimistic): Items successfully persisted to database
    /// 
    /// PROGRESS CALCULATION:
    /// - Uses saved count for accurate progress percentage
    /// - Calculates processing rate (items per second)
    /// - Estimates time remaining based on current rate
    /// 
    /// Called every PROGRESS_REPORT_INTERVAL (5) items for responsive UI updates.
    /// </summary>
    private async Task ReportProgressWithCumulativeCount(
        string jobId,
        int cumulativeProcessingCount,
        int cumulativeSavedCount,
        string context,
        DateTime batchStartTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the ACTUAL cumulative job status from cache
            var cacheKey = $"job_status_{jobId}";
            var jobStatus = _memoryCache.Get<ConnectivityAssessmentJobStatusDto>(cacheKey);
            
            if (jobStatus == null)
            {
                _logger.LogWarning("Job {JobId} status not found in cache during progress reporting", jobId);
                return;
            }

            var actualTotalCount = jobStatus.TotalCount ?? 1; // Default to 1 to avoid division by zero
            var jobStartTime = jobStatus.StartTime.HasValue ? jobStatus.StartTime.Value : DateTime.UtcNow;
            var elapsed = DateTime.UtcNow - jobStartTime;
            
            var processingPercentage = actualTotalCount > 0 
                ? Math.Round((double)cumulativeProcessingCount / (double)actualTotalCount * 100.0, 1) 
                : 0.0;
            
            var savedPercentage = actualTotalCount > 0 
                ? Math.Round((double)cumulativeSavedCount / (double)actualTotalCount * 100.0, 1) 
                : 0.0;
            
            var rate = elapsed.TotalSeconds > 0 
                ? (double)cumulativeSavedCount / elapsed.TotalSeconds 
                : 0.0;

            var remainingSeconds = (cumulativeSavedCount > 0 && rate > 0)
                ? ((double)actualTotalCount - (double)cumulativeSavedCount) / rate
                : 0.0;

            var estimatedRemaining = remainingSeconds > 0 
                ? TimeSpan.FromSeconds(remainingSeconds)
                : TimeSpan.Zero;

            // Show both processing (optimistic) and saved (pessimistic) counts for transparency
            var progressMessage = $"Processing {context}: Processing {cumulativeProcessingCount} (Saved: {cumulativeSavedCount})/{actualTotalCount} " +
                $"({savedPercentage}% saved) - Rate: {rate:F1}/sec - ETA: {estimatedRemaining:mm\\:ss}";

            // Always update the job status cache with both counts
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing,
                progressMessage, 
                processingCount: cumulativeProcessingCount,
                processedCount: cumulativeSavedCount, 
                totalCount: actualTotalCount,
                cancellationToken: cancellationToken);

            // Log progress every 5 saved items or at completion milestones
            if (cumulativeSavedCount % PROGRESS_REPORT_INTERVAL == 0 || 
                cumulativeSavedCount == actualTotalCount)
            {
                _logger.LogInformation("Job {JobId}: {ProgressMessage}", jobId, progressMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress for job {JobId}", jobId);
            // Don't rethrow as this is auxiliary functionality
        }
    }

    /// <summary>
    /// Legacy method - kept for backward compatibility but redirects to new implementation
    /// Assumes processing count equals saved count for backward compatibility
    /// </summary>
    [Obsolete("Use ReportProgressWithCumulativeCount instead")]
    private async Task ReportProgress(
        string jobId,
        int currentBatchProcessedCount,
        int currentBatchTotalCount,
        string context,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
        // Legacy calls assume processing count == saved count
        await ReportProgressWithCumulativeCount(jobId, currentBatchProcessedCount, currentBatchProcessedCount, 
            context, startTime, cancellationToken);
    }

    /// <summary>
    /// Performs memory management operations to prevent memory exhaustion during long-running jobs.
    /// 
    /// OPERATIONS:
    /// - Forces garbage collection (Gen 0, 1, 2)
    /// - Logs current memory usage
    /// - Provides visibility into memory consumption patterns
    /// 
    /// Called every MEMORY_CHECK_INTERVAL (5) batches.
    /// Essential for large hierarchical jobs processing thousands of locations.
    /// </summary>
    private async Task PerformMemoryManagement(string jobId, int batchIndex)
    {
        try
        {
            var memoryBefore = GC.GetTotalMemory(false) / 1024 / 1024; // MB

            if (memoryBefore > 500) // 500MB threshold
            {
                _logger.LogDebug("Job {JobId}: Memory pressure detected ({MemoryMB} MB), forcing garbage collection at batch {BatchIndex}",
                    jobId, memoryBefore, batchIndex);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                _logger.LogDebug("Job {JobId}: Garbage collection completed. Memory: {Before} MB -> {After} MB (freed {Freed} MB)",
                    jobId, memoryBefore, memoryAfter, memoryBefore - memoryAfter);
            }

            // Clear memory cache entries that might be stale
            _memoryCache.Remove($"assessment_cache_" + jobId);

            await Task.Delay(10); // Small delay to allow GC to complete
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory management for job {JobId} at batch {BatchIndex}", jobId, batchIndex);
            // Don't rethrow as this is auxiliary functionality
        }
    }

    /// <summary>
    /// Updates an existing assessment entity with newly calculated data.
    /// 
    /// UPDATED FIELDS:
    /// - All CMTS scores and connectivity status
    /// - Infrastructure counts (cells, NAPs, WiFi, providers)
    /// - Speed test statistics
    /// - Coverage percentages and ratios
    /// - LastUpdated timestamp and AssessedBy user
    /// 
    /// PRESERVED FIELDS:
    /// - AssessmentDate (original creation date)
    /// - LocationId and LocationType (identity fields)
    /// </summary>
    private void UpdateExistingAssessment(ConnectivityAssessment existing, ConnectivityAssessment newData)
    {
        try
        {
            existing.LastUpdated = DateTime.UtcNow;
            existing.AssessmentDate = newData.AssessmentDate;
            existing.ConnectivityStatus = newData.ConnectivityStatus;
            existing.TotalScore = newData.TotalScore;
            existing.FiberCoverageScore = newData.FiberCoverageScore;
            existing.InternetAccessScore = newData.InternetAccessScore;
            existing.TechnologyTypeScore = newData.TechnologyTypeScore;
            existing.QualityOfServiceScore = newData.QualityOfServiceScore;
            existing.CoverageAvailabilityScore = newData.CoverageAvailabilityScore;
            existing.AverageDownloadSpeedMbps = newData.AverageDownloadSpeedMbps;
            existing.AverageUploadSpeedMbps = newData.AverageUploadSpeedMbps;
            existing.AverageLatencyMs = newData.AverageLatencyMs;
            existing.MedianDownloadSpeedMbps = newData.MedianDownloadSpeedMbps;
            existing.MedianUploadSpeedMbps = newData.MedianUploadSpeedMbps;
            existing.SpeedTestCount = newData.SpeedTestCount;
            existing.AverageSignalStrength = newData.AverageSignalStrength;
            existing.MeetsMinimumSpeedRequirement = newData.MeetsMinimumSpeedRequirement;
            existing.MobileCoveragePercentage = newData.MobileCoveragePercentage;
            existing.FixedBroadbandCoveragePercentage = newData.FixedBroadbandCoveragePercentage;
            existing.FiberCoveragePercentage = newData.FiberCoveragePercentage;
            existing.EstimatedPopulationCoveragePercent = newData.EstimatedPopulationCoveragePercent;
            existing.EstimatedHouseholdCoveragePercent = newData.EstimatedHouseholdCoveragePercent;
            existing.AreaSizeKm2 = newData.AreaSizeKm2;
            existing.Population = newData.Population;
            existing.HouseholdCount = newData.HouseholdCount;
            existing.CellTowerCount = newData.CellTowerCount;
            existing.FiberNAPCount = newData.FiberNAPCount;
            existing.WifiHotspotCount = newData.WifiHotspotCount;
            existing.DominantServiceProvider = newData.DominantServiceProvider;
            existing.ServiceProviderCount = newData.ServiceProviderCount;
            existing.HasReliableConnectivity = newData.HasReliableConnectivity;
            existing.AssessedBy = newData.AssessedBy;
            existing.Notes = newData.Notes;
            
            // CRITICAL: Update wireless coverage fields to persist new wireless data
            existing.WirelessCoverageJson = newData.WirelessCoverageJson;
            existing.WirelessCoverageLastUpdated = newData.WirelessCoverageLastUpdated;

            _logger.LogDebug("Updated existing assessment for {LocationType} {LocationId}. WirelessCoverage updated: {HasWireless}",
                existing.LocationType, existing.LocationId, newData.WirelessCoverageJson != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating existing assessment for {LocationType} {LocationId}",
                existing.LocationType, existing.LocationId);
            throw;
        }
    }

    /// <summary>
    /// Creates a LocationConnectivityDto from a ConnectivityAssessment entity for API response.
    /// 
    /// MAPPING:
    /// - Extracts all assessment properties
    /// - Adds location name for display
    /// - Formats scores and percentages
    /// - Includes technology and speed statistics
    /// 
    /// Used to construct the final result list returned by the job.
    /// </summary>
    private LocationConnectivityDto CreateLocationConnectivityDto(ConnectivityAssessment assessment, string locationName)
    {
        try
        {
            return new()
            {
                LocationId = assessment.LocationId,
                LocationName = locationName,
                LocationType = assessment.LocationType,
                ConnectivityStatus = assessment.ConnectivityStatus,
                TotalScore = assessment.TotalScore,
                CMTSScores = new()
                {
                    FiberCoverage = assessment.FiberCoverageScore,
                    InternetAccess = assessment.InternetAccessScore,
                    TechnologyType = assessment.TechnologyTypeScore,
                    QualityOfService = assessment.QualityOfServiceScore,
                    CoverageAvailability = assessment.CoverageAvailabilityScore
                },
                Infrastructure = new()
                {
                    FiberNAPCount = assessment.FiberNAPCount,
                    ServiceProviderCount = assessment.ServiceProviderCount,
                    CellTowerCount = assessment.CellTowerCount,
                    WiFiHotspotCount = assessment.WifiHotspotCount,
                    DominantServiceProvider = assessment.DominantServiceProvider
                },
                SpeedStats = new()
                {
                    AverageDownloadSpeedMbps = assessment.AverageDownloadSpeedMbps,
                    AverageUploadSpeedMbps = assessment.AverageUploadSpeedMbps,
                    AverageLatencyMs = assessment.AverageLatencyMs,
                    SpeedTestCount = assessment.SpeedTestCount,
                    MeetsMinimumSpeedRequirement = assessment.MeetsMinimumSpeedRequirement
                },
                Coverage = new()
                {
                    MobileCoveragePercentage = assessment.MobileCoveragePercentage,
                    FixedBroadbandCoveragePercentage = assessment.FixedBroadbandCoveragePercentage,
                    FiberCoveragePercentage = assessment.FiberCoveragePercentage,
                    AssetsPerThousandPeople = assessment.AssetsPerThousandPeople,
                    ProvidersPerThousandPeople = assessment.ProvidersPerThousandPeople,
                    EstimatedPopulationCoveragePercent = assessment.EstimatedPopulationCoveragePercent,
                    EstimatedHouseholdCoveragePercent = assessment.EstimatedHouseholdCoveragePercent,
                    HasReliableConnectivity = assessment.HasReliableConnectivity
                },
                AreaSizeKm2 = assessment.AreaSizeKm2,
                CenterPoint = assessment.Location != null ? new PointDto
                {
                    Latitude = assessment.Location.Y,
                    Longitude = assessment.Location.X
                } : null,
                Population = assessment.Population,
                HouseholdCount = assessment.HouseholdCount,
                AssessmentDate = assessment.AssessmentDate,
                LastUpdated = assessment.LastUpdated,
                Notes = $"Generated by background job on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating LocationConnectivityDto for {LocationType} {LocationId}",
                assessment.LocationType, assessment.LocationId);
            throw;
        }
    }

    /// <summary>
    /// Determines connectivity status based on total CMTS score (0-15 points).
    /// 
    /// CLASSIFICATION:
    /// - Served: 11-15 points (strong infrastructure and service quality)
    /// - Underserved: 6-10 points (moderate infrastructure, needs improvement)
    /// - Unserved: 0-5 points (minimal or no connectivity infrastructure)
    /// </summary>
    private static ConnectivityStatusEnum DetermineConnectivityStatus(int totalScore)
    {
        return totalScore switch
        {
            >= 11 => ConnectivityStatusEnum.Served,        // 11-15 points
            >= 6 => ConnectivityStatusEnum.Underserved,    // 6-10 points  
            _ => ConnectivityStatusEnum.Unserved           // =5 points
        };
    }

    /// <summary>
    /// Determines if a location has reliable connectivity based on infrastructure and performance.
    /// 
    /// RELIABILITY CRITERIA:
    /// Must have infrastructure (total assets > 0) AND one of:
    /// - Multiple service providers (provider competition improves reliability)
    /// - Good speeds (meets 25 Mbps minimum requirement)
    /// 
    /// Single provider with poor speeds is considered unreliable.
    /// </summary>
    private static bool DetermineReliableConnectivity(InfrastructureStatsDto infrastructure, SpeedStatsDto? speedStats)
    {
        var hasInfrastructure = infrastructure.TotalAssets > 0;
        var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;
        var hasGoodSpeeds = speedStats?.MeetsMinimumSpeedRequirement == true;

        return hasInfrastructure && (hasMultipleProviders || hasGoodSpeeds);
    }

    /// <summary>
    /// Verifies that assessments were actually persisted to the database.
    /// 
    /// PURPOSE:
    /// - Confirms successful database writes
    /// - Detects discrepancies between processed and saved counts
    /// - Logs warnings if database state doesn't match expectations
    /// 
    /// Called at job completion before marking status as Complete.
    /// 
    /// RETURN VALUE:
    /// Count of assessments found in database for the specified locations.
    /// 
    /// OPTIMIZATION:
    /// - Processes location IDs in batches to prevent timeout on large datasets
    /// - Uses dedicated timeout (10 minutes) separate from main job timeout
    /// - Batch size configurable via VERIFICATION_BATCH_SIZE constant
    /// </summary>
    private async Task<int> VerifyDatabasePersistence(
        List<Guid> locationIds,
        LocationTypeEnum locationType,
        CancellationToken cancellationToken)
    {
        // Create a linked cancellation token with dedicated timeout for verification
        using var verificationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        verificationCts.CancelAfter(TimeSpan.FromMinutes(VERIFICATION_TIMEOUT_MINUTES));
        var verificationToken = verificationCts.Token;
        
        try
        {
            // Process in batches to prevent timeout on large datasets (e.g., 3000+ barangays)
            var totalCount = 0;
            var batches = (int)Math.Ceiling((double)locationIds.Count / (double)VERIFICATION_BATCH_SIZE);
            
            _logger.LogDebug(
                "Verifying {TotalCount} {LocationType} assessments in {BatchCount} batches of {BatchSize} (timeout: {Timeout} min)",
                locationIds.Count, locationType, batches, VERIFICATION_BATCH_SIZE, VERIFICATION_TIMEOUT_MINUTES);
            
            for (int i = 0; i < batches; i++)
            {
                var batchIds = locationIds.Skip(i * VERIFICATION_BATCH_SIZE).Take(VERIFICATION_BATCH_SIZE).ToList();
                var dbAssessments = await _connectivityRepository.GetByLocationIdsAsync(
                    batchIds, locationType, verificationToken);
                totalCount += dbAssessments?.Count ?? 0;
                
                _logger.LogDebug(
                    "Verified batch {BatchNum}/{TotalBatches}: {BatchCount} assessments found (elapsed: {Elapsed:F1}s)",
                    i + 1, batches, dbAssessments?.Count ?? 0, 
                    verificationCts.Token.IsCancellationRequested ? -1 : (DateTime.UtcNow - DateTime.UtcNow).TotalSeconds);
            }
            
            return totalCount;
        }
        catch (OperationCanceledException)
        {
            // Distinguish between verification timeout and main job cancellation
            if (verificationToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Database verification timed out after {Timeout} minutes for {LocationType} locations ({Count} IDs). " +
                    "Consider increasing VERIFICATION_TIMEOUT_MINUTES or VERIFICATION_BATCH_SIZE.",
                    VERIFICATION_TIMEOUT_MINUTES, locationType, locationIds.Count);
            }
            else
            {
                _logger.LogWarning(
                    "Database verification canceled for {LocationType} locations due to job cancellation.",
                    locationType);
            }
            return 0; // Return 0 instead of throwing to allow graceful completion
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error verifying database persistence for {LocationType} locations ({Count} IDs)", 
                locationType, locationIds.Count);
            return 0; // Return 0 instead of throwing to allow graceful completion with warning
        }
    }

    /// <summary>
    /// Processes hierarchical assessments for child locations with performance controls and safety measures
    /// </summary>
    private async Task<(List<LocationConnectivityDto> Results, int SkippedCount)> ProcessHierarchicalAssessments(
        List<Guid> locationIds,
        LocationTypeEnum locationType,
        int hierarchicalDepth,
        bool forceRecalculate,
        string jobId,
        int currentProcessedCount,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var results = new List<LocationConnectivityDto>();

        try
        {
            _logger.LogInformation("Job {JobId}: Starting hierarchical processing for {LocationCount} {LocationType}s with depth {Depth}",
                jobId, locationIds.Count, locationType, hierarchicalDepth);

            // Get child locations based on location type and depth
            var childLocations = await GetChildLocationsAsync(locationIds, locationType, hierarchicalDepth, cancellationToken);

            if (childLocations.Count == 0)
            {
                _logger.LogInformation("Job {JobId}: No child locations found for hierarchical processing", jobId);
                return (results, 0);
            }

            _logger.LogInformation("Job {JobId}: Found {ChildCount} child locations for hierarchical processing", jobId, childLocations.Count);

            // Apply performance limit ONLY if explicitly set by user
            int? maxLocations = null;
            try
            {
                var cacheKey = $"job_request_{jobId}";
                if (_memoryCache.TryGetValue(cacheKey, out ConnectivityAssessmentJobRequestDto? jobRequest) && jobRequest != null)
                {
                    maxLocations = jobRequest.MaxHierarchicalLocations;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting max hierarchical locations for job {JobId}", jobId);
            }

            if (maxLocations.HasValue && childLocations.Count > maxLocations.Value)
            {
                _logger.LogWarning("Job {JobId}: Hierarchical processing limited to {MaxCount} locations (found {TotalCount} locations). " +
                    "This is a user-configured limit. Some locations will not be processed.",
                    jobId, maxLocations.Value, childLocations.Count);
                childLocations = childLocations.Take(maxLocations.Value).ToList();
            }
            else if (maxLocations.HasValue)
            {
                _logger.LogInformation("Job {JobId}: Processing all {Count} hierarchical locations (limit: {Limit})",
                    jobId, childLocations.Count, maxLocations.Value);
            }
            else
            {
                _logger.LogInformation("Job {JobId}: Processing all {Count} hierarchical locations (no limit)",
                    jobId, childLocations.Count);
            }

            var hierarchicalProcessingResult = await ProcessLocationsSequentially(
                childLocations.Select(l => (l.Id, l.Type, l.Name)).ToList(),
                forceRecalculate,
                jobId,
                "hierarchical locations",
                startDate,
                endDate,
                null, // No pre-computed wireless data in legacy call
                cancellationToken);

            results.AddRange(hierarchicalProcessingResult.Results);

            _logger.LogInformation("Job {JobId}: Completed hierarchical processing. Generated {ResultCount} assessments from {ChildCount} child locations (Skipped: {Skipped})",
                jobId, results.Count, childLocations.Count, hierarchicalProcessingResult.SkippedCount);

            return (results, hierarchicalProcessingResult.SkippedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Error in hierarchical processing. Continuing with completed primary assessments.", jobId);
            return (results, 0);
        }
    }

    /// <summary>
    /// Retrieves all child locations for a list of parent locations based on hierarchical depth.
    /// 
    /// HIERARCHICAL STRUCTURE:
    /// - Regional → Provincial (depth 1) → Municipal (depth 2) → Barangay (depth 3)
    /// - Provincial → Municipal (depth 1) → Barangay (depth 2)
    /// - Municipal → Barangay (depth 1)
    /// - Barangay → None (leaf level)
    /// 
    /// RETURN VALUE:
    /// List of tuples containing (LocationId, LocationType, LocationName) for all discovered child locations.
    /// </summary>
    private async Task<List<(Guid Id, LocationTypeEnum Type, string Name)>> GetChildLocationsAsync(
        List<Guid> locationIds,
        LocationTypeEnum parentType,
        int hierarchicalDepth,
        CancellationToken cancellationToken)
    {
        var childLocations = new List<(Guid Id, LocationTypeEnum Type, string Name)>();

        try
        {
            foreach (var locationId in locationIds)
            {
                var locationChildren = await GetChildLocationsByTypeAsync(locationId, parentType, hierarchicalDepth, cancellationToken);
                childLocations.AddRange(locationChildren);
            }

            return childLocations
                .GroupBy(l => new { l.Id, l.Type })
                .Select(g => g.First())
                .OrderBy(l => l.Type)
                .ThenBy(l => l.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child locations for {ParentType} hierarchical processing", parentType);
            return childLocations;
        }
    }

    /// <summary>
    /// Retrieves child locations for a single parent location based on type and hierarchical depth.
    /// 
    /// DEPTH HANDLING:
    /// - Depth 1: Immediate children only
    /// - Depth 2: Children and grandchildren
    /// - Depth 3: Full hierarchy to barangay level
    /// 
    /// Recursively traverses the hierarchy up to the specified depth.
    /// </summary>
    private async Task<List<(Guid Id, LocationTypeEnum Type, string Name)>> GetChildLocationsByTypeAsync(
        Guid locationId,
        LocationTypeEnum parentType,
        int hierarchicalDepth,
        CancellationToken cancellationToken)
    {
        var results = new List<(Guid Id, LocationTypeEnum Type, string Name)>();

        try
        {
            switch (parentType)
            {
                case LocationTypeEnum.Regional:
                    var provinces = await _provinceRepository.ToListAsync(
                        _provinceRepository.QueryAsNoTracking(p => p.RegionId == locationId)
                            .Select(p => new { p.Id, p.Name }), cancellationToken);

                    results.AddRange(provinces.Select(p => (p.Id, LocationTypeEnum.Provincial, p.Name)));

                    if (hierarchicalDepth >= 2)
                    {
                        foreach (var province in provinces)
                        {
                            var municipalities = await _municipalityRepository.ToListAsync(
                                _municipalityRepository.QueryAsNoTracking(m => m.ProvinceId == province.Id)
                                    .Select(m => new { m.Id, m.Name }), cancellationToken);

                            results.AddRange(municipalities.Select(m => (m.Id, LocationTypeEnum.Municipality, m.Name)));
                        }
                    }

                    if (hierarchicalDepth >= 3)
                    {
                        var municipalityIds = results.Where(r => r.Type == LocationTypeEnum.Municipality).Select(r => r.Id).ToList();

                        foreach (var municipalityId in municipalityIds)
                        {
                            var barangays = await _barangayRepository.ToListAsync(
                                _barangayRepository.QueryAsNoTracking(b => b.MunicipalityId == municipalityId)
                                    .Select(b => new { b.Id, b.Name }), cancellationToken);

                            results.AddRange(barangays.Select(b => (b.Id, LocationTypeEnum.Barangay, b.Name)));
                        }
                    }
                    break;

                case LocationTypeEnum.Provincial:
                    var provinceMunicipalities = await _municipalityRepository.ToListAsync(
                        _municipalityRepository.QueryAsNoTracking(m => m.ProvinceId == locationId)
                            .Select(m => new { m.Id, m.Name }), cancellationToken);

                    results.AddRange(provinceMunicipalities.Select(m => (m.Id, LocationTypeEnum.Municipality, m.Name)));

                    if (hierarchicalDepth >= 2)
                    {
                        var municipalityIds = provinceMunicipalities.Select(m => m.Id).ToList();

                        foreach (var municipalityId in municipalityIds)
                        {
                            var barangays = await _barangayRepository.ToListAsync(
                                _barangayRepository.QueryAsNoTracking(b => b.MunicipalityId == municipalityId)
                                    .Select(b => new { b.Id, b.Name }), cancellationToken);

                            results.AddRange(barangays.Select(b => (b.Id, LocationTypeEnum.Barangay, b.Name)));
                        }
                    }
                    break;

                case LocationTypeEnum.Municipality:
                    var municipalityBarangays = await _barangayRepository.ToListAsync(
                        _barangayRepository.QueryAsNoTracking(b => b.MunicipalityId == locationId)
                            .Select(b => new { b.Id, b.Name }), cancellationToken);

                    results.AddRange(municipalityBarangays.Select(b => (b.Id, LocationTypeEnum.Barangay, b.Name)));
                    break;

                case LocationTypeEnum.Barangay:
                    // Barangays have no children
                    break;
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting children for {ParentType} {LocationId}", parentType, locationId);
            return results;
        }
    }

    /// <summary>
    /// Applies subquery-based boundary filtering without materializing geometry objects.
    /// 
    /// PURPOSE:
    /// - Keeps spatial operations in the database using correlated subqueries
    /// - Prevents timeout issues from loading large boundary polygons into memory
    /// - Essential for Regional and Provincial queries with complex boundaries
    /// 
    /// USAGE:
    /// Accepts optional location IDs for region, province, municipality, or barangay.
    /// Only one parameter should be provided. The method automatically selects
    /// the appropriate repository and applies the boundary filter.
    /// 
    /// EXAMPLE:
    /// <code>
    /// var cellQuery = _cellRepository.QueryAsNoTracking();
    /// cellQuery = await ApplyLocationBoundaryFilterAsync(cellQuery, regionId: someRegionId, cancellationToken: ct);
    /// var actualCount = await cellQuery.CountAsync(ct); // Fast, no materialization
    /// </code>
    /// </summary>
    private async Task<IQueryable<TSource>> ApplyLocationBoundaryFilterAsync<TSource>(
        IQueryable<TSource> query,
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        Guid? barangayId = null,
        CancellationToken cancellationToken = default)
        where TSource : IGeoLocatedEntity
    {
        if (barangayId.HasValue)
        {
            return await GeoQueryHelper.ApplyBoundaryFilterAsync(
                new List<Guid> { barangayId.Value }, 
                _barangayRepository, 
                query, 
                cancellationToken);
        }
        
        if (municipalityId.HasValue)
        {
            return await GeoQueryHelper.ApplyBoundaryFilterAsync(
                new List<Guid> { municipalityId.Value }, 
                _municipalityRepository, 
                query, 
                cancellationToken);
        }
        
        if (provinceId.HasValue)
        {
            return await GeoQueryHelper.ApplyBoundaryFilterAsync(
                new List<Guid> { provinceId.Value }, 
                _provinceRepository, 
                query, 
                cancellationToken);
        }
        
        if (regionId.HasValue)
        {
            return await GeoQueryHelper.ApplyBoundaryFilterAsync(
                new List<Guid> { regionId.Value }, 
                _regionRepository, 
                query, 
                cancellationToken);
        }
        
        // No location specified, return unfiltered query
        return query;
    }

    /// <summary>
    /// Calculates infrastructure statistics with optimized boundary handling and accurate asset counting.
    /// 
    /// OPTIMIZATION STRATEGY:
    /// - Regional/Provincial: Subquery-based filtering (no boundary materialization, prevents timeout)
    /// - Municipal/Barangay: Envelope filtering (single boundary materialization, faster than subquery)
    /// 
    /// COUNTING APPROACH:
    /// - Executes COUNT(*) queries at database level for accurate asset counts
    /// - Retrieves distinct provider names separately for provider breakdown
    /// - Separates actual counts from provider distribution data
    /// 
    /// RETURN DATA:
    /// - Actual asset counts: CellTowerCount, FiberNAPCount, WiFiHotspotCount
    /// - Service provider count: Unique provider names
    /// - Dominant provider: Provider with most assets
    /// - Technology breakdown: 5G/4G/3G/2G distribution
    /// - Provider breakdown: Per-provider asset distribution
    /// </summary>
    private async Task<InfrastructureStatsDto> CalculateInfrastructureWithBoundaryOptimization(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        List<string> cellProviders, napProviders, wifiProviders, wirelessValidations;
        int actualCellCount, actualNapCount, actualWifiCount;
        TechnologyBandBreakdownDto? technologyBandBreakdown = null;

        if (boundary != null)
        {
            var cellQuery = _cellRepository.QueryAsNoTracking();
            
            // Use conditional filtering: subquery for large areas, envelope for small areas
            if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
            {
                _logger.LogInformation("{LocationType} {LocationId}: Using subquery-based filtering (large area)", locationType, locationId);
                cellQuery = locationType == LocationTypeEnum.Regional
                    ? await ApplyLocationBoundaryFilterAsync(cellQuery, regionId: locationId, cancellationToken: cancellationToken)
                    : await ApplyLocationBoundaryFilterAsync(cellQuery, provinceId: locationId, cancellationToken: cancellationToken);
            }
            else
            {
                _logger.LogDebug("{LocationType} {LocationId}: Using envelope filter (small area)", locationType, locationId);
                cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);
            }
            
            try
            {
                // Get actual cell count using optimized query (no materialization)
                actualCellCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .CountAsync(cellQuery, cancellationToken);
                _logger.LogInformation("Actual cell count for {LocationType} {LocationId}: {Count}", 
                    locationType, locationId, actualCellCount);
                
                // Get provider names for provider breakdown (always use distinct to minimize data transfer)
                cellProviders = await _cellRepository.ToListAsync(
                    cellQuery.Select(c => c.ServiceProvider.Provider.Name).Distinct(), cancellationToken);
            }
            catch (Exception cellEx)
            {
                _logger.LogError(cellEx, "Error querying cells for location {LocationId} ({LocationType}). Query: Select ServiceProvider.Provider.Name. Error: {ErrorMessage}",
                    locationId, locationType, cellEx.Message);
                throw;
            }

            var napQuery = _napRepository.QueryAsNoTracking();
            
            // Use conditional filtering: subquery for large areas, envelope for small areas
            if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
            {
                napQuery = locationType == LocationTypeEnum.Regional
                    ? await ApplyLocationBoundaryFilterAsync(napQuery, regionId: locationId, cancellationToken: cancellationToken)
                    : await ApplyLocationBoundaryFilterAsync(napQuery, provinceId: locationId, cancellationToken: cancellationToken);
            }
            else
            {
                napQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, napQuery);
            }
            
            try
            {
                // Get actual NAP count using optimized query (no materialization)
                actualNapCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .CountAsync(napQuery, cancellationToken);
                _logger.LogInformation("Actual NAP count for {LocationType} {LocationId}: {Count}", 
                    locationType, locationId, actualNapCount);
                
                // Get provider names for provider breakdown (always use distinct to minimize data transfer)
                napProviders = await _napRepository.ToListAsync(
                    napQuery.Select(n => n.ServiceProvider.Provider.Name).Distinct(), cancellationToken);
            }
            catch (Exception napEx)
            {
                _logger.LogError(napEx, "Error querying NAPs for location {LocationId} ({LocationType}). Query: Select ServiceProvider.Provider.Name. Error: {ErrorMessage}",
                    locationId, locationType, napEx.Message);
                throw;
            }

            var wifiQuery = _wifiRepository.QueryAsNoTracking();
            
            // Use conditional filtering: subquery for large areas, envelope for small areas
            if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
            {
                wifiQuery = locationType == LocationTypeEnum.Regional
                    ? await ApplyLocationBoundaryFilterAsync(wifiQuery, regionId: locationId, cancellationToken: cancellationToken)
                    : await ApplyLocationBoundaryFilterAsync(wifiQuery, provinceId: locationId, cancellationToken: cancellationToken);
            }
            else
            {
                wifiQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, wifiQuery);
            }
            
            try
            {
                // Get actual WiFi count using optimized query (no materialization)
                actualWifiCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .CountAsync(wifiQuery, cancellationToken);
                _logger.LogInformation("Actual WiFi count for {LocationType} {LocationId}: {Count}", 
                    locationType, locationId, actualWifiCount);
                
                // Get provider names for provider breakdown (always use distinct to minimize data transfer)
                wifiProviders = await _wifiRepository.ToListAsync(
                    wifiQuery.Select(w => w.ServiceProvider.Provider.Name).Distinct(), cancellationToken);
            }
            catch (Exception wifiEx)
            {
                _logger.LogError(wifiEx, "Error querying WiFi hotspots for location {LocationId} ({LocationType}). Query: Select ServiceProvider.Provider.Name. Error: {ErrorMessage}",
                    locationId, locationType, wifiEx.Message);
                throw;
            }

            // Get wireless validations with conditional filtering
            IQueryable<WirelessValidation> wirelessValidationQuery;
            
            if (locationType == LocationTypeEnum.Regional || locationType == LocationTypeEnum.Provincial)
            {
                // Large areas: use barangay ID subquery to avoid boundary materialization
                IQueryable<Guid> barangayIds = locationType == LocationTypeEnum.Regional
                    ? _barangayRepository.QueryAsNoTracking()
                        .Where(b => b.Municipality != null && b.Municipality.Province != null && b.Municipality.Province.RegionId == locationId)
                        .Select(b => b.Id)
                    : _barangayRepository.QueryAsNoTracking()
                        .Where(b => b.Municipality != null && b.Municipality.ProvinceId == locationId)
                        .Select(b => b.Id);
                
                wirelessValidationQuery = _wirelessValidationRepository.QueryAsNoTracking()
                    .Where(wv => barangayIds.Contains(wv.BarangayId)
                               && wv.ServiceProvider != null
                               && wv.ServiceProvider.Provider != null);
            }
            else
            {
                // Small areas: use Within() with materialized boundary (faster than correlated subquery)
                wirelessValidationQuery = _wirelessValidationRepository.QueryAsNoTracking()
                    .Where(wv => wv.Barangay != null
                               && wv.Barangay.Center != null
                               && wv.Barangay.Center.Within(boundary!)
                               && wv.ServiceProvider != null
                               && wv.ServiceProvider.Provider != null);
            }

            // Apply date range filter if provided
            if (startDate.HasValue)
            {
                wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
            }

            wirelessValidations = await _wirelessValidationRepository.ToListAsync(
                wirelessValidationQuery.Select(wv => wv.ServiceProvider.Provider.Name), cancellationToken);

            // Technology-band breakdown with caching (replaces separate technology and band breakdowns)
            var techBandCacheKey = $"tech_band:{locationType}:{locationId}";
            if (!forceRecalculate && _memoryCache.TryGetValue(techBandCacheKey, out TechnologyBandBreakdownDto? cachedTechBand) && cachedTechBand != null)
            {
                technologyBandBreakdown = cachedTechBand;
            }
            else
            {
                if (forceRecalculate)
                {
                    _logger.LogInformation("Force recalculating technology-band breakdown for {LocationType} {LocationId}", locationType, locationId);
                }
                
                technologyBandBreakdown = await AnalyzeTechnologyBandBreakdownAsync(locationId, locationType, boundary, center, areaKm2, true, startDate, endDate, cancellationToken);
                _memoryCache.Set(techBandCacheKey, technologyBandBreakdown, TimeSpan.FromHours(12));
            }
        }
        else
        {
            // Fallback for cases where boundaries couldn't be loaded (very large areas)
            _logger.LogWarning("No boundary available for {LocationType} {LocationId}, using center-point approximation", locationType, locationId);

            var searchRadiusKm = Math.Sqrt(areaKm2 / Math.PI); // Equivalent area circle radius
            var searchRadiusMeters = searchRadiusKm * 1000;

            // Get infrastructure using distance-based queries
            var cellQuery = _cellRepository.QueryAsNoTracking()
                .Where(c => c.Location.Distance(center) <= searchRadiusMeters);
            actualCellCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .CountAsync(cellQuery, cancellationToken);
            cellProviders = await _cellRepository.ToListAsync(
                cellQuery.Select(c => c.ServiceProvider.Provider.Name).Distinct(), cancellationToken);

            var napQuery = _napRepository.QueryAsNoTracking()
                .Where(n => n.Location.Distance(center) <= searchRadiusMeters);
            actualNapCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .CountAsync(napQuery, cancellationToken);
            napProviders = await _napRepository.ToListAsync(
                napQuery.Select(n => n.ServiceProvider.Provider.Name).Distinct(), cancellationToken);

            var wifiQuery = _wifiRepository.QueryAsNoTracking()
                .Where(w => w.Location.Distance(center) <= searchRadiusMeters);
            actualWifiCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .CountAsync(wifiQuery, cancellationToken);
            wifiProviders = await _wifiRepository.ToListAsync(
                wifiQuery.Select(w => w.ServiceProvider.Provider.Name).Distinct(), cancellationToken);

            // Get wireless validation technology data using barangay center distance
            var wirelessValidationQuery = _wirelessValidationRepository.QueryAsNoTracking()
                .Where(wv => wv.Barangay != null
                           && wv.Barangay.Center != null
                           && wv.Barangay.Center.Distance(center) <= searchRadiusMeters
                           && wv.ServiceProvider != null
                           && wv.ServiceProvider.Provider != null);

            // Apply date range filter if provided
            if (startDate.HasValue)
            {
                wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                wirelessValidationQuery = wirelessValidationQuery.Where(wv => wv.CreatedAt <= endOfDay);
            }

            wirelessValidations = await _wirelessValidationRepository.ToListAsync(
                wirelessValidationQuery.Select(wv => wv.ServiceProvider.Provider.Name), cancellationToken);

            // Calculate consolidated technology-band breakdown for radius-based queries
            technologyBandBreakdown = await AnalyzeTechnologyBandBreakdownAsync(locationId, locationType, null, center, areaKm2, false, startDate, endDate, cancellationToken);
        }

        // Include wireless validations in provider list
        var allProviders = cellProviders.Concat(napProviders).Concat(wifiProviders).Concat(wirelessValidations).Where(p => !string.IsNullOrEmpty(p)).ToList();
        var uniqueProviders = allProviders.Distinct().ToList();
        var dominantProvider = allProviders.GroupBy(p => p).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;

        // Calculate provider breakdown - Note: This uses provider names for breakdown percentage, not individual asset counts per provider
        var providerBreakdown = CalculateProviderBreakdown(cellProviders, napProviders, wifiProviders, wirelessValidations, actualCellCount, actualNapCount, actualWifiCount);

        // Log final aggregated counts
        _logger.LogInformation(
            "Infrastructure aggregation complete for {LocationType} {LocationId}: " +
            "ACTUAL asset counts - Cells={CellCount}, NAPs={NAPCount}, WiFi={WiFiCount}, " +
            "Unique providers={ProviderCount}, Provider breakdown entries={BreakdownCount}",
            locationType, locationId,
            actualCellCount, actualNapCount, actualWifiCount,
            uniqueProviders.Count, providerBreakdown.Count);

        return new()
        {
            CellTowerCount = actualCellCount,
            FiberNAPCount = actualNapCount,
            WiFiHotspotCount = actualWifiCount,
            ServiceProviderCount = uniqueProviders.Count,
            DominantServiceProvider = dominantProvider,
            TechnologyBandBreakdown = technologyBandBreakdown,
            ProviderBreakdown = providerBreakdown
        };
    }

    /// <summary>
    /// Calculates detailed provider breakdown showing asset distribution per provider.
    /// 
    /// COUNTING METHODOLOGY:
    /// - Provider-level counts: Distribution based on provider name occurrence in query results
    /// - Total asset counts: Accurate (passed as actualCellCount, actualNapCount, actualWifiCount)
    /// - Asset percentages: Calculated using accurate total counts
    /// 
    /// NOTE:
    /// Individual provider asset counts represent distribution ratios, while
    /// total counts across all providers are accurate database counts.
    /// 
    /// RETURN VALUE:
    /// List of providers ordered by total asset count descending, with percentage of total infrastructure.
    /// </summary>
    private static List<ProviderBreakdownDto> CalculateProviderBreakdown(
        List<string> cellProviders, 
        List<string> napProviders, 
        List<string> wifiProviders, 
        List<string> wirelessValidations,
        int actualCellCount,
        int actualNapCount,
        int actualWifiCount)
    {
        // Group providers and calculate their distribution
        var providerAssets = new Dictionary<string, (int Cells, int NAPs, int WiFi)>();

        // Count occurrences of each provider in cell data (for distribution ratio)
        foreach (var provider in cellProviders.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (!providerAssets.ContainsKey(provider))
                providerAssets[provider] = (0, 0, 0);
   
            var current = providerAssets[provider];
            providerAssets[provider] = (current.Cells + 1, current.NAPs, current.WiFi);
        }

        // Count occurrences of each provider in NAP data (for distribution ratio)
        foreach (var provider in napProviders.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (!providerAssets.ContainsKey(provider))
                providerAssets[provider] = (0, 0, 0);
            
            var current = providerAssets[provider];
            providerAssets[provider] = (current.Cells, current.NAPs + 1, current.WiFi);
        }

        // Count occurrences of each provider in WiFi data (for distribution ratio)
        foreach (var provider in wifiProviders.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (!providerAssets.ContainsKey(provider))
                providerAssets[provider] = (0, 0, 0);
    
            var current = providerAssets[provider];
            providerAssets[provider] = (current.Cells, current.NAPs, current.WiFi + 1);
        }

        var totalAssets = actualCellCount + actualNapCount + actualWifiCount;

        // Create breakdown DTOs ordered by total asset count descending
        var breakdown = providerAssets
            .Select(kvp => new ProviderBreakdownDto
            {
                ProviderName = kvp.Key,
                CellTowerCount = kvp.Value.Cells,
                FiberNAPCount = kvp.Value.NAPs,
                WiFiHotspotCount = kvp.Value.WiFi,
                AssetPercentage = totalAssets > 0
                    ? Math.Round(((double)(kvp.Value.Cells + kvp.Value.NAPs + kvp.Value.WiFi) / totalAssets) * 100, 2)
                    : 0
            })
            .OrderByDescending(p => p.TotalAssetCount)
       .ToList();

        return breakdown;
    }
}
