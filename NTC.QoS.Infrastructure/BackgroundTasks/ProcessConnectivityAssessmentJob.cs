using Hangfire;
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

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job for processing connectivity assessments without blocking API requests
/// Optimized for sequential processing to prevent Entity Framework concurrency issues
/// </summary>
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
    private readonly ICellularRepository _cellularRepository;
    private readonly INAPRepository _napRepository;
    private readonly IWiFiHotspotRepository _wifiRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IWirelessValidationRepository _wirelessValidationRepository;


    // Simplified configuration for sequential processing
    private const int BATCH_SIZE = 20; // Optimal batch size for sequential processing
    private const int PROGRESS_REPORT_INTERVAL = 10; // Report progress every 10 items
    private const int MEMORY_CHECK_INTERVAL = 5; // Check memory every 5 batches
    private const int RECENT_ASSESSMENT_DAYS = 30; // Consider assessments within 30 days as recent

    public ProcessConnectivityAssessmentJob(
        ILogger<ProcessConnectivityAssessmentJob> logger,
        IConnectivityAssessmentRepository connectivityRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<Province> provinceRepository,
        IRepository<Region> regionRepository,
        IMemoryCache memoryCache,
        ICellRepository cellRepository,
        ICellularRepository cellularRepository,
        INAPRepository napRepository,
        IWiFiHotspotRepository wifiRepository,
        ISpeedtestRepository speedtestRepository,
        IWirelessValidationRepository wirelessValidationRepository)
    {
        _logger = logger;
        _connectivityRepository = connectivityRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _memoryCache = memoryCache;
        _cellRepository = cellRepository;
        _cellularRepository = cellularRepository;
        _napRepository = napRepository;
        _wifiRepository = wifiRepository;
        _speedtestRepository = speedtestRepository;
        _wirelessValidationRepository = wirelessValidationRepository;
    }

    /// <summary>
    /// Execute connectivity assessment generation as a background job
    /// </summary>
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(ConnectivityAssessmentJobRequestDto jobRequest, CancellationToken cancellationToken)
    {
        var jobId = jobRequest.JobId;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting connectivity assessment job {JobId} for {LocationCount} {LocationType}s. " +
                "Hierarchical: {Hierarchical}, Enabled: {Enabled}, Depth: {Depth}, Force: {Force}, MaxHierarchical: {MaxHierarchical}",
                jobId, jobRequest.LocationIds.Count, jobRequest.LocationType, 
                jobRequest.GenerateHierarchicalAssessments, jobRequest.EnableHierarchicalProcessing, 
                jobRequest.HierarchicalDepth, jobRequest.ForceRecalculate, jobRequest.MaxHierarchicalLocations);

            // Cache the job request for later use in hierarchical processing
            var jobRequestCacheKey = $"job_request_{jobId}";
            _memoryCache.Set(jobRequestCacheKey, jobRequest, TimeSpan.FromHours(2));

            // Calculate total expected locations for progress tracking
            var totalLocations = jobRequest.LocationIds.Count;
            if (jobRequest.GenerateHierarchicalAssessments && jobRequest.EnableHierarchicalProcessing)
            {
                // Add estimated hierarchical locations based on the logic from StartConnectivityAssessmentJobHandler
                // But cap it at the MaxHierarchicalLocations limit
                var estimatedChildrenPerLocation = jobRequest.LocationType switch
                {
                    LocationTypeEnum.Regional when jobRequest.HierarchicalDepth >= 3 => 1000,
                    LocationTypeEnum.Regional when jobRequest.HierarchicalDepth >= 2 => 100,
                    LocationTypeEnum.Regional => 20,
                    LocationTypeEnum.Provincial when jobRequest.HierarchicalDepth >= 2 => 500,
                    LocationTypeEnum.Provincial => 50,
                    LocationTypeEnum.Municipality => 30,
                    _ => 0
                };
                
                var estimatedHierarchicalTotal = jobRequest.LocationIds.Count * estimatedChildrenPerLocation;
                var cappedHierarchicalTotal = Math.Min(estimatedHierarchicalTotal, jobRequest.MaxHierarchicalLocations);
                totalLocations += cappedHierarchicalTotal;
            }

            // Update job status to Processing with startTime and total count
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing, 
                "Starting assessment generation", 
                startTime: startTime, processedCount: 0, estimatedTotalCount: totalLocations, 
                cancellationToken: cancellationToken);

            var results = new List<LocationConnectivityDto>();
            
            // Process primary locations
            var primaryResults = await ProcessLocationsSequentially(
                jobRequest.LocationIds.Select(id => (id, jobRequest.LocationType, "Primary")).ToList(),
                jobRequest.ForceRecalculate,
                jobId,
                "primary locations",
                cancellationToken);
            
            results.AddRange(primaryResults);

            // Update progress after primary locations
            var progressMessage = jobRequest.GenerateHierarchicalAssessments && jobRequest.EnableHierarchicalProcessing
                ? $"Completed {results.Count} primary assessments. Starting hierarchical processing..."
                : $"Completed {results.Count} primary assessments. Hierarchical processing disabled.";
            
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing, 
                progressMessage, processedCount: results.Count, estimatedTotalCount: totalLocations, 
                cancellationToken: cancellationToken);

            // Process hierarchical assessments ONLY if both flags are enabled
            if (jobRequest.GenerateHierarchicalAssessments && jobRequest.EnableHierarchicalProcessing)
            {
                _logger.LogInformation("Job {JobId}: Hierarchical processing is enabled. Processing child locations...", jobId);
                
                var hierarchicalResults = await ProcessHierarchicalAssessments(
                    jobRequest.LocationIds,
                    jobRequest.LocationType, 
                    jobRequest.HierarchicalDepth,
                    jobRequest.ForceRecalculate,
                    jobId,
                    results.Count, // Start counting from primary results
                    cancellationToken);
                
                results.AddRange(hierarchicalResults);
            }
            else if (jobRequest.GenerateHierarchicalAssessments && !jobRequest.EnableHierarchicalProcessing)
            {
                _logger.LogInformation("Job {JobId}: Hierarchical processing is requested but disabled for performance. " +
                    "To enable, set EnableHierarchicalProcessing=true. Processing only primary locations.", jobId);
            }
            else
            {
                _logger.LogInformation("Job {JobId}: Hierarchical processing not requested. Processing only primary locations.", jobId);
            }

            // Clean up cached job request
            _memoryCache.Remove(jobRequestCacheKey);

            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;
            var successMessage = $"Successfully generated {results.Count} connectivity assessments in {totalTime:mm\\:ss}";
            
            // Update job status to Completed with all tracking info
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Completed, successMessage, 
                endTime: endTime, processedCount: results.Count, estimatedTotalCount: totalLocations, 
                cancellationToken: cancellationToken);
            
            _logger.LogInformation("Job {JobId} completed successfully. {Message}", jobId, successMessage);
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var errorMessage = $"Error in job {jobId}: {ex.Message}";
            _logger.LogError(ex, "Connectivity assessment job {JobId} failed", jobId);
            
            // Clean up cached job request on error
            var jobRequestCacheKey = $"job_request_{jobId}";
            _memoryCache.Remove(jobRequestCacheKey);
            
            // Update job status to Failed with endTime
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Failed, errorMessage, 
                endTime: endTime, cancellationToken: cancellationToken);
            throw; // Re-throw for Hangfire retry logic
        }
    }

    /// <summary>
    /// Process locations sequentially with optimized batching
    /// </summary>
    private async Task<List<LocationConnectivityDto>> ProcessLocationsSequentially(
        List<(Guid Id, LocationTypeEnum Type, string Name)> locations,
        bool forceRecalculate,
        int jobId,
        string processingContext,
        CancellationToken cancellationToken)
    {
        if (locations.Count == 0)
            return new List<LocationConnectivityDto>();

        _logger.LogInformation("Job {JobId}: Processing {LocationCount} {Context}",
            jobId, locations.Count, processingContext);

        var results = new List<LocationConnectivityDto>();
        var startTime = DateTime.UtcNow;

        // Pre-load existing assessments if not forcing recalculation
        var existingAssessments = await PreloadExistingAssessments(locations, forceRecalculate, jobId, cancellationToken);

        // Process locations in batches
        var totalBatches = (int)Math.Ceiling((double)locations.Count / BATCH_SIZE);
        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = locations
                .Skip(batchIndex * BATCH_SIZE)
                .Take(BATCH_SIZE)
                .ToList();

            try
            {
                var batchResults = await ProcessBatch(batch, forceRecalculate, existingAssessments, cancellationToken);
                results.AddRange(batchResults.Where(r => r != null)!);

                // Report progress with updated processed count
                await ReportProgress(jobId, results.Count, locations.Count, processingContext, startTime, cancellationToken);

                // Memory management
                if (batchIndex % MEMORY_CHECK_INTERVAL == 0 && batchIndex > 0)
                {
                    await PerformMemoryManagement(jobId, batchIndex);
                }

                // Small delay to prevent database pressure
                if (batchIndex < totalBatches - 1)
                {
                    await Task.Delay(50, cancellationToken); // 50ms delay between batches
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Job {JobId}: Error processing batch {BatchNumber}/{TotalBatches}. Continuing with next batch.", 
                    jobId, batchIndex + 1, totalBatches);
                continue; // Continue with next batch
            }
        }

        var totalElapsed = DateTime.UtcNow - startTime;
        _logger.LogInformation("Job {JobId}: Completed processing {LocationCount} {Context}. " +
            "Generated {ResultCount} assessments in {ElapsedTime:mm\\:ss}. Rate: {Rate:F1}/sec",
            jobId, locations.Count, processingContext, results.Count, totalElapsed, 
            totalElapsed.TotalSeconds > 0 ? results.Count / totalElapsed.TotalSeconds : 0);

        return results;
    }

    /// <summary>
    /// Pre-load existing assessments to optimize database access
    /// </summary>
    private async Task<Dictionary<(Guid, LocationTypeEnum), ConnectivityAssessment>> PreloadExistingAssessments(
        List<(Guid Id, LocationTypeEnum Type, string Name)> locations,
        bool forceRecalculate,
        int jobId,
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
    /// Process a batch of locations sequentially
    /// </summary>
    private async Task<List<LocationConnectivityDto?>> ProcessBatch(
        List<(Guid Id, LocationTypeEnum Type, string Name)> batchLocations,
        bool forceRecalculate,
        Dictionary<(Guid, LocationTypeEnum), ConnectivityAssessment> existingAssessments,
        CancellationToken cancellationToken)
    {
        var results = new List<LocationConnectivityDto?>();
        
        foreach (var location in batchLocations)
        {
            try
            {
                var result = await ProcessSingleLocation(
                    location.Id, 
                    location.Type, 
                    location.Name,
                    forceRecalculate,
                    existingAssessments,
                    cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate assessment for {LocationType} {LocationId} ({LocationName})", 
                    location.Type, location.Id, location.Name);
                results.Add(null);
            }
        }

        return results;
    }

    /// <summary>
    /// Process a single location assessment
    /// </summary>
    private async Task<LocationConnectivityDto?> ProcessSingleLocation(
        Guid locationId, 
        LocationTypeEnum locationType, 
        string locationName,
        bool forceRecalculate,
        Dictionary<(Guid, LocationTypeEnum), ConnectivityAssessment> existingAssessments,
        CancellationToken cancellationToken)
    {
        try
        {
            ConnectivityAssessment? existing = null;
            
            // First check the pre-loaded cache
            if (existingAssessments.TryGetValue((locationId, locationType), out existing))
            {
                // If not forcing recalculation and assessment is recent, use it
                if (!forceRecalculate && existing.LastUpdated > DateTime.UtcNow.AddDays(-RECENT_ASSESSMENT_DAYS))
                {
                    _logger.LogDebug("Using existing recent assessment for {LocationType} {LocationId}", locationType, locationId);
                    return CreateLocationConnectivityDto(existing, locationName);
                }
            }
            else
            {
                // If not in cache, check database directly to avoid duplicate key errors
                existing = await _connectivityRepository.GetByLocationIdAsync(locationId, locationType, cancellationToken);
                
                // If found and not forcing recalculation and is recent, use it
                if (existing != null && !forceRecalculate && existing.LastUpdated > DateTime.UtcNow.AddDays(-RECENT_ASSESSMENT_DAYS))
                {
                    _logger.LogDebug("Using existing recent assessment (from DB) for {LocationType} {LocationId}", locationType, locationId);
                    return CreateLocationConnectivityDto(existing, locationName);
                }
            }

            // Generate new assessment
            _logger.LogDebug("Generating new assessment for {LocationType} {LocationId} (existing: {HasExisting}, force: {ForceRecalculate})", 
                locationType, locationId, existing != null, forceRecalculate);
            
            var newAssessmentData = await GenerateNewAssessment(locationId, locationType, forceRecalculate, cancellationToken);
            
            if (existing != null)
            {
                // Update existing assessment
                UpdateExistingAssessment(existing, newAssessmentData);
                _connectivityRepository.Update(existing);
                
                await _connectivityRepository.SaveChangesAsync(cancellationToken);
                return CreateLocationConnectivityDto(existing, locationName);
            }
            else
            {
                // Create new assessment
                await _connectivityRepository.AddAsync(newAssessmentData, cancellationToken);
                await _connectivityRepository.SaveChangesAsync(cancellationToken);
                
                return CreateLocationConnectivityDto(newAssessmentData, locationName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing assessment for {LocationType} {LocationId}", locationType, locationId);
            return null;
        }
    }

    /// <summary>
    /// Generate a complete connectivity assessment with full CMTS rubric implementation
    /// Updated to align with GenerateConnectivityAssessmentHandler implementation
    /// </summary>
    private async Task<ConnectivityAssessment> GenerateNewAssessment(
        Guid locationId, 
        LocationTypeEnum locationType,
        bool forceRecalculate,
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
                    AssessedBy = "BackgroundJob"
                };
            }

            // Calculate infrastructure statistics within the boundary (with caching)
            var infrastructureStats = await CalculateInfrastructureStatsAsync(boundary, center, areaKm2, locationId, locationType, forceRecalculate, cancellationToken);
            
            // Calculate speed test statistics (with caching)
            var speedStats = await CalculateSpeedTestStatsAsync(boundary, center, areaKm2, locationId, locationType, forceRecalculate, cancellationToken);
            
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
                technologyBreakdown = await AnalyzeTechnologyGenerationsAsync(boundary, center, areaKm2, true, cancellationToken);
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
                AssessedBy = "BackgroundJob",
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
                ProvidersPerThousandPeople = coverage.ProvidersPerThousandPeople
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
            _barangayRepository.Query(b => b.Id == barangayId)
                .Select(b => new { b.Name, b.Center, b.Area, b.TotalPopulation, b.HouseholdCount }),
            cancellationToken);

        if (barangayInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Always load boundaries for barangays - they're typically small and accuracy is important
        Geometry? boundary = null;
        var boundaryQuery = _barangayRepository.Query(b => b.Id == barangayId)
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
            _municipalityRepository.Query(m => m.Id == municipalityId)
                .Select(m => new { m.Name, m.Center, m.Area, m.TotalPopulation, m.HouseholdCount }),
            cancellationToken);

        if (municipalityInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Always load boundaries for accurate government mapping - performance optimized with caching
        Geometry? boundary = null;
        var boundaryQuery = _municipalityRepository.Query(m => m.Id == municipalityId)
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
            _provinceRepository.Query(p => p.Id == provinceId)
                .Select(p => new { p.Name, p.Center, p.Area, p.TotalPopulation, p.HouseholdCount }),
            cancellationToken);

        if (provinceInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Always load boundaries for accurate government mapping - performance optimized with caching
        Geometry? boundary = null;
        var boundaryQuery = _provinceRepository.Query(p => p.Id == provinceId)
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
            _regionRepository.Query(r => r.Id == regionId)
                .Select(r => new { r.Name, r.Center, r.Area, r.TotalPopulation, r.HouseholdCount }),
            cancellationToken);

        if (regionInfo == null)
            return ("Unknown", null, null, 0, null, null);

        // Load boundaries for accurate government mapping - large regions benefit most from caching
        Geometry? boundary = null;
        var boundaryQuery = _regionRepository.Query(r => r.Id == regionId)
            .Select(r => r.Boundaries);
        boundary = await _regionRepository.FirstOrDefaultAsync(boundaryQuery, cancellationToken);

        return (regionInfo.Name, boundary, regionInfo.Center, regionInfo.Area,
               regionInfo.TotalPopulation, regionInfo.HouseholdCount);
    }

    /// <summary>
    /// Enhanced infrastructure calculation with multi-level caching and accurate boundary usage
    /// </summary>
    private async Task<InfrastructureStatsDto> CalculateInfrastructureStatsAsync(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, CancellationToken cancellationToken)
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

        _logger.LogDebug("Calculating infrastructure stats for {LocationType} {LocationId} (Area: {Area} km²)", locationType, locationId, areaKm2);

        // Use accurate boundaries with optimizations
        var result = await CalculateInfrastructureWithBoundaryOptimization(boundary, center, areaKm2, locationId, locationType, forceRecalculate, cancellationToken);

        // Always cache the fresh result
        var infraCacheKey = $"infra:{locationType}:{locationId}";
        _memoryCache.Set(infraCacheKey, result, TimeSpan.FromHours(24));

        return result;
    }

    /// <summary>
    /// Enhanced speed stats calculation with caching
    /// </summary>
    private async Task<SpeedStatsDto?> CalculateSpeedTestStatsAsync(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, CancellationToken cancellationToken)
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
            // Use accurate boundary with envelope optimization
            var query = _speedtestRepository.Query();
            query = GeoQueryHelper.ApplyEnvelopeFilter(boundary, query);

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
        else
        {
            // Radius-based fallback
            var searchRadiusKm = Math.Sqrt(areaKm2 / Math.PI);
            var searchRadiusMeters = searchRadiusKm * 1000;

            var speedtestResults = await _speedtestRepository.ToListAsync(
                _speedtestRepository.Query()
                    .Where(s => s.Location.Distance(center) <= searchRadiusMeters)
                    .Select(s => new SpeedtestDto
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

        // Always cache the fresh result
        if (result != null)
        {
            var speedCacheKey = $"speed:{locationType}:{locationId}";
            _memoryCache.Set(speedCacheKey, result, TimeSpan.FromHours(4));
        }

        return result;
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
    private async Task<TechnologyBreakdownDto> AnalyzeTechnologyGenerationsAsync(Geometry? boundary, Point center, double areaKm2, bool useBoundary, CancellationToken cancellationToken)
    {
        try
        {
            var allTechData = new List<(string GenerationName, string TechnologyStandard, Guid Id)>();

            if (useBoundary && boundary != null)
            {
                // Use boundary-based queries for speedtest data
                var speedtestQuery = _speedtestRepository.Query();
                speedtestQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, speedtestQuery);
                var speedtestResults = await _speedtestRepository.ToListAsync(
                    speedtestQuery
                        .Where(s => s.Generation != null && s.Technology != null)
                        .Select(s => new {
                            GenerationName = s.Generation!.Name,
                            TechnologyStandard = s.Technology!.Standard,
                            s.Id
                        }), cancellationToken);

                allTechData.AddRange(speedtestResults.Select(s => (s.GenerationName, s.TechnologyStandard, s.Id)));

                // Get cellular technology data using cell repository with spatial filter
                var cellQuery = _cellRepository.Query();
                cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);

                var cellularResults = await _cellRepository.ToListAsync(
                    cellQuery
                        .SelectMany(cell => cell.CellSectors!)
                        .SelectMany(sector => sector.Cellulars!)
                        .Where(c => c.Technology != null && c.Technology.Generation != null)
                        .Select(c => new {
                            GenerationName = c.Technology!.Generation!.Name,
                            TechnologyStandard = c.Technology!.Standard,
                            c.Id
                        }), cancellationToken);

                allTechData.AddRange(cellularResults.Select(c => (c.GenerationName, c.TechnologyStandard, c.Id)));

                // Get wireless validation technology data
                // WirelessValidation doesn't have a Location property, so we filter using Barangay.Center
                var wirelessValidationResults = await _wirelessValidationRepository.ToListAsync(
                    _wirelessValidationRepository.Query()
                        .Where(wv => wv.Barangay != null
                                   && wv.Barangay.Center != null
                                   && boundary.Intersects(wv.Barangay.Center)
                                   && wv.Technology != null
                                   && wv.Technology.Generation != null)
                        .Select(wv => new {
                            GenerationName = wv.Technology!.Generation!.Name,
                            TechnologyStandard = wv.Technology!.Standard,
                            wv.Id
                        }), cancellationToken);

                allTechData.AddRange(wirelessValidationResults.Select(wv => (wv.GenerationName, wv.TechnologyStandard, wv.Id)));
            }
            else
            {
                // Use radius-based queries for larger areas
                var searchRadiusKm = Math.Min(Math.Sqrt(areaKm2 / Math.PI), 50);
                var searchRadiusMeters = searchRadiusKm * 1000;

                var speedtestResults = await _speedtestRepository.ToListAsync(
                    _speedtestRepository.Query()
                        .Where(s => s.Location.Distance(center) <= searchRadiusMeters
                                  && s.Generation != null && s.Technology != null)
                        .Select(s => new {
                            GenerationName = s.Generation!.Name,
                            TechnologyStandard = s.Technology!.Standard,
                            s.Id
                        }), cancellationToken);

                allTechData.AddRange(speedtestResults.Select(s => (s.GenerationName, s.TechnologyStandard, s.Id)));

                var cellularResults = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => c.CellSector!.Cell.Location.Distance(center) <= searchRadiusMeters)
                        .Select(c => new {
                            GenerationName = c.Technology!.Generation!.Name,
                            TechnologyStandard = c.Technology!.Standard,
                            c.Id
                        }), cancellationToken);

                allTechData.AddRange(cellularResults.Select(c => (c.GenerationName, c.TechnologyStandard, c.Id)));

                // Get wireless validation technology data using barangay center distance
                var wirelessValidationResults = await _wirelessValidationRepository.ToListAsync(
                    _wirelessValidationRepository.Query()
                        .Where(wv => wv.Barangay != null
                                   && wv.Barangay.Center != null
                                   && wv.Barangay.Center.Distance(center) <= searchRadiusMeters
                                   && wv.Technology != null
                                   && wv.Technology.Generation != null)
                        .Select(wv => new {
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
            return "Unknown";

        var normalized = generationName.ToUpperInvariant().Trim();

        return normalized switch
        {
            var s when s.Contains("2G") || s.Contains("GSM") || s.Contains("EDGE") => "2G",
            var s when s.Contains("3G") || s.Contains("UMTS") || s.Contains("WCDMA") || s.Contains("HSPA") => "3G",
            var s when s.Contains("4G") || s.Contains("LTE") || s.Contains("LTE-A") => "4G",
            var s when s.Contains("5G") || s.Contains("NR") || s.Contains("5G NR") => "5G",
            _ => normalized
        };
    }

    /// <summary>
    /// Calculate median value from ordered list
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
    /// Calculate CMTS scores based on infrastructure, speed data, and population
    /// Enhanced with technology breakdown support
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
        var isHighDensityArea = populationDensity > 500; // >500 people per km² = high density
        var isUrbanArea = populationDensity > 1000; // >1000 people per km² = urban

        return (populationDensity, isUrbanArea, isHighDensityArea);
    }

    /// <summary>
    /// Calculate Fiber Coverage Score (0-3 points) - CMTS Criterion 1
    /// </summary>
    private static int CalculateFiberCoverageScore(InfrastructureStatsDto infrastructure, double areaKm2, int? population, (double PopulationDensity, bool IsUrbanArea, bool IsHighDensityArea) context)
    {
        if (infrastructure.FiberNAPCount == 0)
        {
            return 1; // Unserved - No installed or present FTTH facilities
        }

        // Population-aware fiber coverage calculation
        double fiberDensityScore;

        if (population.HasValue && population > 0)
        {
            // Calculate fiber NAPs per 1000 people
            var fiberPer1000People = (infrastructure.FiberNAPCount * 1000.0) / population.Value;

            if (context.IsUrbanArea)
            {
                // Urban areas need higher fiber density
                fiberDensityScore = fiberPer1000People >= 2.0 ? 3 :    // 2+ NAPs per 1000 people = Served
                                   fiberPer1000People >= 0.5 ? 2 :     // 0.5-2 NAPs per 1000 people = Underserved
                                   1;                                   // <0.5 NAPs per 1000 people = Unserved
            }
            else if (context.IsHighDensityArea)
            {
                // Semi-urban areas
                fiberDensityScore = fiberPer1000People >= 1.0 ? 3 :    // 1+ NAPs per 1000 people = Served
                                   fiberPer1000People >= 0.3 ? 2 :     // 0.3-1 NAPs per 1000 people = Underserved  
                                   1;                                   // <0.3 NAPs per 1000 people = Unserved
            }
            else
            {
                // Rural areas - lower per-capita requirements but consider geographic coverage
                var geographicDensity = areaKm2 > 0 ? infrastructure.FiberNAPCount / areaKm2 : 0;
                var populationDensityScore = fiberPer1000People >= 0.5 ? 3 :
                                           fiberPer1000People >= 0.2 ? 2 : 1;
                var geographicDensityScore = geographicDensity >= 0.1 ? 3 :
                                           geographicDensity >= 0.05 ? 2 : 1;

                // Use the higher of the two scores for rural areas
                fiberDensityScore = Math.Max(populationDensityScore, geographicDensityScore);
            }
        }
        else
        {
            // Fallback to geographic density when population data unavailable
            var geographicDensity = areaKm2 > 0 ? infrastructure.FiberNAPCount / areaKm2 : 0;
            fiberDensityScore = geographicDensity >= 0.1 ? 3 :
                              geographicDensity >= 0.05 ? 2 : 1;
        }

        return (int)Math.Round(fiberDensityScore);
    }

    /// <summary>
    /// Calculate Internet Access Score (0-3 points) - CMTS Criterion 2
    /// </summary>
    private static int CalculateInternetAccessScore(SpeedStatsDto? speedStats)
    {
        if (speedStats == null || speedStats.SpeedTestCount == 0)
        {
            return 1; // No data - assume unserved
        }

        if (speedStats.AverageDownloadSpeedMbps < 25)
        {
            return 1; // Unserved - No internet access or minimal
        }

        if (speedStats.AverageDownloadSpeedMbps >= 25 && speedStats.AverageDownloadSpeedMbps < 100)
        {
            return 2; // Underserved - Available but slow/unreliable
        }

        return 3; // Served - Stable internet meeting/exceeding standards
    }

    /// <summary>
    /// Calculate Technology Type Score (0-3 points) - CMTS Criterion 3
    /// Enhanced with technology breakdown support
    /// </summary>
    private static int CalculateTechnologyTypeScore(InfrastructureStatsDto infrastructure)
    {
        var hasModernInfra = infrastructure.FiberNAPCount > 0 || infrastructure.CellTowerCount > 0;
        var hasDiverseInfra = (infrastructure.FiberNAPCount > 0 ? 1 : 0) +
                             (infrastructure.CellTowerCount > 0 ? 1 : 0) +
                             (infrastructure.WiFiHotspotCount > 0 ? 1 : 0);

        // Enhanced technology scoring using actual generation data
        if (infrastructure.TechnologyBreakdown != null && infrastructure.TechnologyBreakdown.TotalTechnologyCount > 0)
        {
            var tech = infrastructure.TechnologyBreakdown;

            // Score based on generation analysis
            if (tech.Count5G > 0 && tech.HasModernTechnology) // 5G present
            {
                return 3; // Served - 5G/LTE infrastructure active
            }

            if (tech.Count4G > 0 && !tech.IsLegacyDominated) // 4G present and not legacy-dominated
            {
                return 3; // Served - 4G/LTE infrastructure active  
            }

            if (tech.HasModernTechnology && tech.IsLegacyDominated) // Has 4G/5G but legacy dominates
            {
                return 2; // Underserved - Mixed legacy and modern
            }

            if (tech.Count3G > 0 || tech.Count2G > 0) // Only legacy technologies
            {
                return 2; // Underserved - Legacy technologies only
            }

            return 1; // Unserved - No identifiable technology
        }

        // Fallback to infrastructure-based scoring when no technology data available
        if (!hasModernInfra)
        {
            return 1; // Unserved - No installed broadband technology
        }

        if (hasDiverseInfra == 1)
        {
            return 2; // Underserved - Limited infrastructure diversity
        }

        return 3; // Served - Diverse infrastructure present
    }

    /// <summary>
    /// Calculate Quality of Service Score (0-3 points) - CMTS Criterion 4
    /// Enhanced with 5G bonus
    /// </summary>
    private static int CalculateQualityOfServiceScore(SpeedStatsDto? speedStats, InfrastructureStatsDto infrastructure)
    {
        if (speedStats == null || speedStats.SpeedTestCount < 5) // Insufficient data
        {
            return 1; // Assume poor quality without data
        }

        var hasGoodLatency = speedStats.AverageLatencyMs <= 50; // Under 50ms
        var hasConsistentSpeeds = speedStats.MeetsMinimumSpeedRequirement;
        var hasGoodSignal = speedStats.AverageSignalStrength >= 3.5; // Average or better signal

        // Enhanced quality scoring with signal strength consideration
        var qualityFactors = 0;

        if (hasConsistentSpeeds) qualityFactors++;
        if (hasGoodLatency) qualityFactors++;
        if (hasGoodSignal) qualityFactors++;

        // Bonus for 5G areas with excellent performance
        if (infrastructure.TechnologyBreakdown?.Count5G > 0 &&
            speedStats.AverageDownloadSpeedMbps >= 100 &&
            speedStats.AverageLatencyMs <= 20)
        {
            qualityFactors++;
        }

        return qualityFactors switch
        {
            >= 3 => 3, // Served - High quality (good speed, low latency, good signal)
            2 => 2,    // Underserved - Fair quality but some issues
            _ => 1     // Unserved - Poor quality
        };
    }

    /// <summary>
    /// Calculate Coverage Availability Score (0-3 points) - CMTS Criterion 5
    /// Enhanced with modern technology consideration
    /// </summary>
    private static int CalculateCoverageAvailabilityScore(InfrastructureStatsDto infrastructure, double areaKm2, int? population, (double PopulationDensity, bool IsUrbanArea, bool IsHighDensityArea) context)
    {
        if (infrastructure.TotalAssets == 0)
        {
            return 1; // Unserved - No coverage
        }

        double coverageScore;

        if (population.HasValue && population > 0)
        {
            // Population-based coverage calculation (aligns with CMTS rubric)
            var assetsPerCapita = (double)infrastructure.TotalAssets / population.Value;
            var providersPerThousand = (infrastructure.ServiceProviderCount * 1000.0) / population.Value;
            var hasModernTech = infrastructure.TechnologyBreakdown?.HasModernTechnology == true;
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

            // Apply CMTS rubric thresholds
            if (estimatedCoveragePercent >= 100 && hasMultipleProviders && hasModernTech)
            {
                coverageScore = 3; // Served - 100% coverage with competition and modern tech
            }
            else if (estimatedCoveragePercent >= 40 && (hasMultipleProviders || hasModernTech))
            {
                coverageScore = 2; // Underserved - 40-99% coverage with some positive factors
            }
            else if (estimatedCoveragePercent >= 40)
            {
                coverageScore = 2; // Underserved - 40-99% coverage
            }
            else
            {
                coverageScore = 1; // Unserved - <40% coverage
            }
        }
        else
        {
            // Fallback to geographic density when population data unavailable
            var assetDensity = areaKm2 > 0 ? infrastructure.TotalAssets / areaKm2 : 0;
            var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;
            var hasModernTech = infrastructure.TechnologyBreakdown?.HasModernTechnology == true;

            if (hasMultipleProviders && hasModernTech && assetDensity >= 1.0)
            {
                coverageScore = 3; // Served - Comprehensive coverage
            }
            else if (hasMultipleProviders || hasModernTech || assetDensity >= 0.5)
            {
                coverageScore = 2; // Underserved - Partial coverage
            }
            else
            {
                coverageScore = 1; // Unserved - Limited coverage
            }
        }

        return (int)Math.Round(coverageScore);
    }

    /// <summary>
    /// Calculates population-aware coverage statistics
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

            // Estimate household coverage
            if (householdCount.HasValue && householdCount > 0)
            {
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
    /// Updates job status in the database with tracking information
    /// </summary>
    private Task UpdateJobStatus(
        int jobId, 
        ConnectivityAssessmentJobStatusEnum status, 
        string message, 
        DateTime? startTime = null, 
        DateTime? endTime = null, 
        int? processedCount = null, 
        int? estimatedTotalCount = null,
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
                    ProcessedCount = 0,
                    EstimatedTotalCount = estimatedTotalCount ?? 1 // Use provided total or default to 1
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

            if (processedCount.HasValue)
                currentStatus.ProcessedCount = processedCount.Value;

            if (estimatedTotalCount.HasValue)
                currentStatus.EstimatedTotalCount = estimatedTotalCount.Value;

            // Update the cache with the new status (extend expiry to 24 hours)
            _memoryCache.Set(cacheKey, currentStatus, TimeSpan.FromHours(24));

            _logger.LogInformation("Job {JobId} status update: {Status} - {Message}. " +
                "StartTime: {StartTime}, EndTime: {EndTime}, ProcessedCount: {ProcessedCount}",
                jobId, status, message, startTime, endTime, processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {JobId} status to {Status}", jobId, status);
            // Don't rethrow as this is auxiliary functionality
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reports progress during processing
    /// </summary>
    private async Task ReportProgress(
        int jobId, 
        int processedCount, 
        int totalCount, 
        string context, 
        DateTime startTime, 
        CancellationToken cancellationToken)
    {
        try
        {
            var elapsed = DateTime.UtcNow - startTime;
            var percentage = Math.Round((double)processedCount / totalCount * 100, 1);
            var rate = processedCount > 0 && elapsed.TotalSeconds > 0 ? processedCount / elapsed.TotalSeconds : 0;
            
            var estimatedRemaining = processedCount > 0 && rate > 0 
                ? TimeSpan.FromSeconds((totalCount - processedCount) / rate)
                : TimeSpan.Zero;

            var progressMessage = $"Processing {context}: {processedCount}/{totalCount} ({percentage}%) - " +
                $"Rate: {rate:F1}/sec - ETA: {estimatedRemaining:mm\\:ss}";

            _logger.LogInformation("Job {JobId}: {ProgressMessage}", jobId, progressMessage);

            // Report progress every 10 items as configured
            if (processedCount % PROGRESS_REPORT_INTERVAL == 0 || processedCount == totalCount)
            {
                await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing, 
                    progressMessage, processedCount: processedCount, estimatedTotalCount: totalCount, 
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress for job {JobId}", jobId);
            // Don't rethrow as this is auxiliary functionality
        }
    }

    /// <summary>
    /// Performs memory management operations
    /// </summary>
    private async Task PerformMemoryManagement(int jobId, int batchIndex)
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
            _memoryCache.Remove($"assessment_cache_{jobId}");

            await Task.Delay(10); // Small delay to allow GC to complete
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory management for job {JobId} at batch {BatchIndex}", jobId, batchIndex);
            // Don't rethrow as this is auxiliary functionality
        }
    }

    /// <summary>
    /// Updates an existing assessment with new data
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
            existing.FiberNAPCount = newData.FiberNAPCount;
            existing.ServiceProviderCount = newData.ServiceProviderCount;
            existing.HasReliableConnectivity = newData.HasReliableConnectivity;
            existing.AssessedBy = newData.AssessedBy;

            _logger.LogDebug("Updated existing assessment for {LocationType} {LocationId}", 
                existing.LocationType, existing.LocationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating existing assessment for {LocationType} {LocationId}", 
                existing.LocationType, existing.LocationId);
            throw;
        }
    }

    /// <summary>
    /// Creates a LocationConnectivityDto from a ConnectivityAssessment entity
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
    /// Determine connectivity status based on total CMTS score
    /// </summary>
    private static ConnectivityStatusEnum DetermineConnectivityStatus(int totalScore)
    {
        return totalScore switch
        {
            >= 11 => ConnectivityStatusEnum.Served,        // 11-15 points
            >= 6 => ConnectivityStatusEnum.Underserved,    // 6-10 points  
            _ => ConnectivityStatusEnum.Unserved           // ≤5 points
        };
    }

    /// <summary>
    /// Determine if location has reliable connectivity
    /// </summary>
    private static bool DetermineReliableConnectivity(InfrastructureStatsDto infrastructure, SpeedStatsDto? speedStats)
    {
        var hasInfrastructure = infrastructure.TotalAssets > 0;
        var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;
        var hasGoodSpeeds = speedStats?.MeetsMinimumSpeedRequirement == true;

        return hasInfrastructure && (hasMultipleProviders || hasGoodSpeeds);
    }

    /// <summary>
    /// Processes hierarchical assessments for child locations with performance controls and safety measures
    /// </summary>
    private async Task<List<LocationConnectivityDto>> ProcessHierarchicalAssessments(
        List<Guid> locationIds,
        LocationTypeEnum locationType,
        int hierarchicalDepth,
        bool forceRecalculate,
        int jobId,
        int currentProcessedCount,
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
                return results;
            }

            _logger.LogInformation("Job {JobId}: Found {ChildCount} child locations for hierarchical processing", jobId, childLocations.Count);

            // Apply performance limits
            var maxLocations = 1000; // Default safety limit
            try
            {
                var cacheKey = $"job_request_{jobId}";
                if (_memoryCache.TryGetValue(cacheKey, out ConnectivityAssessmentJobRequestDto? jobRequest))
                {
                    maxLocations = jobRequest.MaxHierarchicalLocations;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting max hierarchical locations for job {JobId}, using default", jobId);
            }
            
            if (childLocations.Count > maxLocations)
            {
                _logger.LogWarning("Job {JobId}: Hierarchical processing limited to {MaxCount} locations (requested {RequestedCount} locations)",
                    jobId, maxLocations, childLocations.Count);
                childLocations = childLocations.Take(maxLocations).ToList();
            }

            var hierarchicalResults = await ProcessLocationsSequentially(
                childLocations.Select(l => (l.Id, l.Type, l.Name)).ToList(),
                forceRecalculate,
                jobId,
                "hierarchical locations",
                cancellationToken);

            results.AddRange(hierarchicalResults);

            _logger.LogInformation("Job {JobId}: Completed hierarchical processing. Generated {ResultCount} assessments from {ChildCount} child locations",
                jobId, results.Count, childLocations.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Error in hierarchical processing. Continuing with completed primary assessments.", jobId);
            return results;
        }
    }

    /// <summary>
    /// Get child locations based on parent location type and hierarchical depth
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
    /// Get child locations for a specific parent location based on type and depth
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
                        _provinceRepository.Query(p => p.RegionId == locationId)
                            .Select(p => new { p.Id, p.Name }), cancellationToken);
                    
                    results.AddRange(provinces.Select(p => (p.Id, LocationTypeEnum.Provincial, p.Name)));

                    if (hierarchicalDepth >= 2)
                    {
                        foreach (var province in provinces)
                        {
                            var municipalities = await _municipalityRepository.ToListAsync(
                                _municipalityRepository.Query(m => m.ProvinceId == province.Id)
                                    .Select(m => new { m.Id, m.Name }), cancellationToken);
                            
                            results.AddRange(municipalities.Select(m => (m.Id, LocationTypeEnum.Municipality, m.Name)));
                        }
                    }

                    if (hierarchicalDepth >= 3)
                    {
                        var municipalityIds = results.Where(r => r.Type == LocationTypeEnum.Municipality).Select(r => r.Id).Take(50).ToList();
                        
                        foreach (var municipalityId in municipalityIds)
                        {
                            var barangays = await _barangayRepository.ToListAsync(
                                _barangayRepository.Query(b => b.MunicipalityId == municipalityId)
                                    .Take(100)
                                    .Select(b => new { b.Id, b.Name }), cancellationToken);
                            
                            results.AddRange(barangays.Select(b => (b.Id, LocationTypeEnum.Barangay, b.Name)));
                        }
                    }
                    break;

                case LocationTypeEnum.Provincial:
                    var provinceMunicipalities = await _municipalityRepository.ToListAsync(
                        _municipalityRepository.Query(m => m.ProvinceId == locationId)
                            .Select(m => new { m.Id, m.Name }), cancellationToken);
                    
                    results.AddRange(provinceMunicipalities.Select(m => (m.Id, LocationTypeEnum.Municipality, m.Name)));

                    if (hierarchicalDepth >= 2)
                    {
                        var municipalityIds = provinceMunicipalities.Select(m => m.Id).Take(50).ToList();
                        
                        foreach (var municipalityId in municipalityIds)
                        {
                            var barangays = await _barangayRepository.ToListAsync(
                                _barangayRepository.Query(b => b.MunicipalityId == municipalityId)
                                    .Take(100)
                                    .Select(b => new { b.Id, b.Name }), cancellationToken);
                            
                            results.AddRange(barangays.Select(b => (b.Id, LocationTypeEnum.Barangay, b.Name)));
                        }
                    }
                    break;

                case LocationTypeEnum.Municipality:
                    var municipalityBarangays = await _barangayRepository.ToListAsync(
                        _barangayRepository.Query(b => b.MunicipalityId == locationId)
                            .Take(200)
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
    /// Smart boundary optimization that maintains accuracy while improving performance
    /// </summary>
    private async Task<InfrastructureStatsDto> CalculateInfrastructureWithBoundaryOptimization(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, CancellationToken cancellationToken)
    {
        List<string> cells, naps, wifiHotspots;
        TechnologyBreakdownDto? technologyBreakdown = null;

        if (boundary != null)
        {
            _logger.LogDebug("Using accurate MultiPolygon boundaries for precise infrastructure counting");

            var cellQuery = _cellRepository.Query();
            cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);
            cells = await _cellRepository.ToListAsync(
                cellQuery.Select(c => c.ServiceProvider.Provider.Name), cancellationToken);

            var napQuery = _napRepository.Query();
            napQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, napQuery);
            naps = await _napRepository.ToListAsync(
                napQuery.Select(n => n.ServiceProvider.Provider.Name), cancellationToken);

            var wifiQuery = _wifiRepository.Query();
            wifiQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, wifiQuery);
            wifiHotspots = await _wifiRepository.ToListAsync(
                wifiQuery.Select(w => w.ServiceProvider.Provider.Name), cancellationToken);

            var techCacheKey = $"tech:{locationType}:{locationId}";
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
                technologyBreakdown = await AnalyzeTechnologyGenerationsAsync(boundary, center, areaKm2, true, cancellationToken);
                _memoryCache.Set(techCacheKey, technologyBreakdown, TimeSpan.FromHours(12));
            }
        }
        else
        {
            // Fallback for cases where boundaries couldn't be loaded (very large areas)
            _logger.LogWarning("No boundary available for {LocationType} {LocationId}, using center-point approximation", locationType, locationId);

            var searchRadiusKm = Math.Sqrt(areaKm2 / Math.PI); // Equivalent area circle radius
            var searchRadiusMeters = searchRadiusKm * 1000;

            // Get infrastructure using distance-based queries
            var cellQuery = _cellRepository.Query()
                .Where(c => c.Location.Distance(center) <= searchRadiusMeters);
            cells = await _cellRepository.ToListAsync(
                cellQuery.Select(c => c.ServiceProvider.Provider.Name), cancellationToken);

            var napQuery = _napRepository.Query()
                .Where(n => n.Location.Distance(center) <= searchRadiusMeters);
            naps = await _napRepository.ToListAsync(
                napQuery.Select(n => n.ServiceProvider.Provider.Name), cancellationToken);

            var wifiQuery = _wifiRepository.Query()
                .Where(w => w.Location.Distance(center) <= searchRadiusMeters);
            wifiHotspots = await _wifiRepository.ToListAsync(
                wifiQuery.Select(w => w.ServiceProvider.Provider.Name), cancellationToken);

            technologyBreakdown = await AnalyzeTechnologyGenerationsAsync(null, center, areaKm2, false, cancellationToken);
        }

        var allProviders = cells.Concat(naps).Concat(wifiHotspots).Where(p => !string.IsNullOrEmpty(p)).ToList();
        var uniqueProviders = allProviders.Distinct().ToList();
        var dominantProvider = allProviders.GroupBy(p => p).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;

        return new()
        {
            CellTowerCount = cells.Count,
            FiberNAPCount = naps.Count,
            WiFiHotspotCount = wifiHotspots.Count,
            ServiceProviderCount = uniqueProviders.Count,
            DominantServiceProvider = dominantProvider,
            TechnologyBreakdown = technologyBreakdown
        };
    }
}