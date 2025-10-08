using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;

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
    private readonly IRepository<NTC.QoS.Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Region> _regionRepository;
    private readonly IMemoryCache _memoryCache;

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
        IRepository<NTC.QoS.Domain.Entities.Province> provinceRepository,
        IRepository<NTC.QoS.Domain.Entities.Region> regionRepository,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _connectivityRepository = connectivityRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Execute connectivity assessment generation as a background job
    /// </summary>
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(ConnectivityAssessmentJobRequest jobRequest, CancellationToken cancellationToken)
    {
        var jobId = jobRequest.JobId;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting connectivity assessment job {JobId} for {LocationCount} {LocationType}s. " +
                "Hierarchical: {Hierarchical}, Depth: {Depth}, Force: {Force}",
                jobId, jobRequest.LocationIds.Count, jobRequest.LocationType, 
                jobRequest.GenerateHierarchicalAssessments, jobRequest.HierarchicalDepth, jobRequest.ForceRecalculate);

            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing, "Starting assessment generation", cancellationToken);

            var results = new List<LocationConnectivityDto>();
            
            // Process primary locations
            var primaryResults = await ProcessLocationsSequentially(
                jobRequest.LocationIds.Select(id => (id, jobRequest.LocationType, "Primary")).ToList(),
                jobRequest.ForceRecalculate,
                jobId,
                "primary locations",
                cancellationToken);
            
            results.AddRange(primaryResults);

            // Process hierarchical assessments if requested
            if (jobRequest.GenerateHierarchicalAssessments)
            {
                await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing, 
                    $"Completed {results.Count} primary assessments. Starting hierarchical processing...", cancellationToken);

                var hierarchicalResults = await ProcessHierarchicalAssessments(
                    jobRequest.LocationIds,
                    jobRequest.LocationType, 
                    jobRequest.HierarchicalDepth,
                    jobRequest.ForceRecalculate,
                    jobId,
                    cancellationToken);
                
                results.AddRange(hierarchicalResults);
            }

            var totalTime = DateTime.UtcNow - startTime;
            var successMessage = $"Successfully generated {results.Count} connectivity assessments in {totalTime:mm\\:ss}";
            
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Completed, successMessage, cancellationToken);
            
            _logger.LogInformation("Job {JobId} completed successfully. {Message}", jobId, successMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error in job {jobId}: {ex.Message}";
            _logger.LogError(ex, "Connectivity assessment job {JobId} failed", jobId);
            
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Failed, errorMessage, cancellationToken);
            throw; // Re-throw for Hangfire retry logic
        }
    }

    /// <summary>
    /// Process locations sequentially with optimized batching
    /// </summary>
    private async Task<List<LocationConnectivityDto>> ProcessLocationsSequentially(
        List<(Guid Id, LocationTypeEnum Type, string Name)> locations,
        bool forceRecalculate,
        string jobId,
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

                // Report progress
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
            // Check for existing assessment
            ConnectivityAssessment? existing = null;
            if (!forceRecalculate && existingAssessments.TryGetValue((locationId, locationType), out existing))
            {
                // Check if existing assessment is recent enough
                if (existing.LastUpdated > DateTime.UtcNow.AddDays(-RECENT_ASSESSMENT_DAYS))
                {
                    _logger.LogDebug("Using existing recent assessment for {LocationType} {LocationId}", locationType, locationId);
                    return CreateLocationConnectivityDto(existing, "Existing");
                }
            }

            // Generate new assessment
            _logger.LogDebug("Generating new assessment for {LocationType} {LocationId}", locationType, locationId);
            
            var assessment = await GenerateNewAssessment(locationId, locationType, cancellationToken);
            
            if (existing != null)
            {
                // Update existing assessment
                UpdateExistingAssessment(existing, assessment);
                _connectivityRepository.Update(existing);
                assessment = existing; // Use updated existing for return
            }
            else
            {
                // Create new assessment
                await _connectivityRepository.AddAsync(assessment, cancellationToken);
            }

            await _connectivityRepository.SaveChangesAsync(cancellationToken);

            return CreateLocationConnectivityDto(assessment, locationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing assessment for {LocationType} {LocationId}", locationType, locationId);
            return null;
        }
    }

    /// <summary>
    /// Generate a new connectivity assessment (placeholder implementation)
    /// </summary>
    private async Task<ConnectivityAssessment> GenerateNewAssessment(
        Guid locationId, 
        LocationTypeEnum locationType, 
        CancellationToken cancellationToken)
    {
        // TODO: Replace with actual assessment logic
        // This is a placeholder implementation
        
        await Task.Delay(10, cancellationToken); // Simulate processing time
        
        return new ConnectivityAssessment
        {
            LocationId = locationId,
            LocationType = locationType,
            AssessmentDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            ConnectivityStatus = ConnectivityStatusEnum.Underserved,
            TotalScore = 5,
            FiberCoverageScore = 1,
            InternetAccessScore = 1,
            TechnologyTypeScore = 1,
            QualityOfServiceScore = 1,
            CoverageAvailabilityScore = 1,
            AssessedBy = "BackgroundJob"
        };
    }

    /// <summary>
    /// Update existing assessment with new values
    /// </summary>
    private static void UpdateExistingAssessment(ConnectivityAssessment existing, ConnectivityAssessment newAssessment)
    {
        existing.LastUpdated = newAssessment.LastUpdated;
        existing.AssessmentDate = newAssessment.AssessmentDate;
        existing.ConnectivityStatus = newAssessment.ConnectivityStatus;
        existing.TotalScore = newAssessment.TotalScore;
        existing.FiberCoverageScore = newAssessment.FiberCoverageScore;
        existing.InternetAccessScore = newAssessment.InternetAccessScore;
        existing.TechnologyTypeScore = newAssessment.TechnologyTypeScore;
        existing.QualityOfServiceScore = newAssessment.QualityOfServiceScore;
        existing.CoverageAvailabilityScore = newAssessment.CoverageAvailabilityScore;
        existing.AssessedBy = newAssessment.AssessedBy;
    }

    /// <summary>
    /// Create LocationConnectivityDto from assessment
    /// </summary>
    private static LocationConnectivityDto CreateLocationConnectivityDto(ConnectivityAssessment assessment, string locationName)
    {
        return new LocationConnectivityDto
        {
            LocationId = assessment.LocationId,
            LocationType = assessment.LocationType,
            LocationName = locationName,
            ConnectivityStatus = assessment.ConnectivityStatus,
            TotalScore = assessment.TotalScore,
            AssessmentDate = assessment.AssessmentDate,
            LastUpdated = assessment.LastUpdated
        };
    }

    /// <summary>
    /// Process hierarchical assessments
    /// </summary>
    private async Task<List<LocationConnectivityDto>> ProcessHierarchicalAssessments(
        List<Guid> parentLocationIds,
        LocationTypeEnum parentLocationType,
        int hierarchicalDepth,
        bool forceRecalculate,
        string jobId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Job {JobId}: Starting hierarchical assessment for {ParentCount} {ParentType}s with depth {Depth}",
                jobId, parentLocationIds.Count, parentLocationType, hierarchicalDepth);

            // Collect all child locations
            var allChildLocations = new List<(Guid Id, LocationTypeEnum Type, string Name)>();
            
            foreach (var parentLocationId in parentLocationIds)
            {
                var childLocations = await GetChildLocationsAsync(parentLocationId, parentLocationType, hierarchicalDepth, cancellationToken);
                allChildLocations.AddRange(childLocations);
            }

            if (allChildLocations.Count == 0)
            {
                _logger.LogInformation("Job {JobId}: No child locations found for hierarchical processing", jobId);
                return new List<LocationConnectivityDto>();
            }

            // Remove duplicates
            var uniqueChildLocations = allChildLocations
                .GroupBy(c => new { c.Id, c.Type })
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Job {JobId}: Found {TotalChildCount} unique child locations (removed {DuplicateCount} duplicates)", 
                jobId, uniqueChildLocations.Count, allChildLocations.Count - uniqueChildLocations.Count);

            // Process child locations
            var childResults = await ProcessLocationsSequentially(
                uniqueChildLocations,
                forceRecalculate,
                jobId,
                "hierarchical child locations",
                cancellationToken);

            _logger.LogInformation("Job {JobId}: Generated {HierarchicalCount} hierarchical assessments",
                jobId, childResults.Count);

            return childResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Error generating hierarchical assessments", jobId);
            throw;
        }
    }

    /// <summary>
    /// Get child locations for hierarchical processing
    /// </summary>
    private async Task<List<(Guid Id, LocationTypeEnum Type, string Name)>> GetChildLocationsAsync(
        Guid parentLocationId,
        LocationTypeEnum parentLocationType,
        int hierarchicalDepth,
        CancellationToken cancellationToken)
    {
        var childLocations = new List<(Guid Id, LocationTypeEnum Type, string Name)>();

        try
        {
            switch (parentLocationType)
            {
                case LocationTypeEnum.Regional:
                    childLocations.AddRange(await GetProvincesInRegionAsync(parentLocationId, cancellationToken));
                    
                    if (hierarchicalDepth >= 2)
                    {
                        var provinceIds = childLocations.Select(c => c.Id).ToList();
                        foreach (var provinceId in provinceIds)
                        {
                            childLocations.AddRange(await GetMunicipalitiesInProvinceAsync(provinceId, cancellationToken));
                        }
                    }
                    
                    if (hierarchicalDepth >= 3)
                    {
                        var municipalityIds = childLocations.Where(c => c.Type == LocationTypeEnum.Municipality).Select(c => c.Id).ToList();
                        foreach (var municipalityId in municipalityIds)
                        {
                            childLocations.AddRange(await GetBarangaysInMunicipalityAsync(municipalityId, cancellationToken));
                        }
                    }
                    break;

                case LocationTypeEnum.Provincial:
                    childLocations.AddRange(await GetMunicipalitiesInProvinceAsync(parentLocationId, cancellationToken));
                    
                    if (hierarchicalDepth >= 2)
                    {
                        var municipalityIds = childLocations.Select(c => c.Id).ToList();
                        foreach (var municipalityId in municipalityIds)
                        {
                            childLocations.AddRange(await GetBarangaysInMunicipalityAsync(municipalityId, cancellationToken));
                        }
                    }
                    break;

                case LocationTypeEnum.Municipality:
                    childLocations.AddRange(await GetBarangaysInMunicipalityAsync(parentLocationId, cancellationToken));
                    break;

                case LocationTypeEnum.Barangay:
                    // Barangays have no children
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting child locations for {ParentType} {ParentId}", parentLocationType, parentLocationId);
        }

        return childLocations;
    }

    /// <summary>
    /// Get provinces in a region
    /// </summary>
    private async Task<List<(Guid Id, LocationTypeEnum Type, string Name)>> GetProvincesInRegionAsync(
        Guid regionId, CancellationToken cancellationToken)
    {
        try
        {
            var provinces = await _provinceRepository.ToListAsync(
                _provinceRepository.Query(p => p.RegionId == regionId)
                    .Select(p => new { p.Id, p.Name }), 
                cancellationToken);

            return provinces.Select(p => (p.Id, LocationTypeEnum.Provincial, p.Name)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting provinces for region {RegionId}", regionId);
            return new List<(Guid Id, LocationTypeEnum Type, string Name)>();
        }
    }

    /// <summary>
    /// Get municipalities in a province
    /// </summary>
    private async Task<List<(Guid Id, LocationTypeEnum Type, string Name)>> GetMunicipalitiesInProvinceAsync(
        Guid provinceId, CancellationToken cancellationToken)
    {
        try
        {
            var municipalities = await _municipalityRepository.ToListAsync(
                _municipalityRepository.Query(m => m.ProvinceId == provinceId)
                    .Select(m => new { m.Id, m.Name }), 
                cancellationToken);

            return municipalities.Select(m => (m.Id, LocationTypeEnum.Municipality, m.Name)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting municipalities for province {ProvinceId}", provinceId);
            return new List<(Guid Id, LocationTypeEnum Type, string Name)>();
        }
    }

    /// <summary>
    /// Get barangays in a municipality
    /// </summary>
    private async Task<List<(Guid Id, LocationTypeEnum Type, string Name)>> GetBarangaysInMunicipalityAsync(
        Guid municipalityId, CancellationToken cancellationToken)
    {
        try
        {
            var barangays = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.MunicipalityId == municipalityId)
                    .Select(b => new { b.Id, b.Name }), 
                cancellationToken);

            return barangays.Select(b => (b.Id, LocationTypeEnum.Barangay, b.Name)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting barangays for municipality {MunicipalityId}", municipalityId);
            return new List<(Guid Id, LocationTypeEnum Type, string Name)>();
        }
    }

    /// <summary>
    /// Report processing progress
    /// </summary>
    private async Task ReportProgress(
        string jobId, 
        int processedCount, 
        int totalCount, 
        string context, 
        DateTime startTime, 
        CancellationToken cancellationToken)
    {
        if (processedCount % PROGRESS_REPORT_INTERVAL == 0 || processedCount == totalCount)
        {
            var progressPercent = Math.Round((double)processedCount / totalCount * 100, 1);
            var elapsed = DateTime.UtcNow - startTime;
            var estimatedRemaining = processedCount > 0 
                ? TimeSpan.FromTicks(elapsed.Ticks * (totalCount - processedCount) / processedCount)
                : TimeSpan.Zero;

            var progressMessage = $"Processed {processedCount}/{totalCount} {context} ({progressPercent}%) - ETA: {estimatedRemaining:mm\\:ss}";
            
            await UpdateJobStatus(jobId, ConnectivityAssessmentJobStatusEnum.Processing, progressMessage, cancellationToken);
            
            _logger.LogInformation("Job {JobId}: {ProgressMessage}", jobId, progressMessage);
        }
    }

    /// <summary>
    /// Update job status in memory cache
    /// </summary>
    private async Task UpdateJobStatus(string jobId, ConnectivityAssessmentJobStatusEnum status, string message, CancellationToken cancellationToken)
    {
        try
        {
            var jobStatus = new NTC.QoS.Domain.Models.DTOs.Analytics.ConnectivityAssessmentJobStatus
            {
                JobId = jobId,
                Status = status,
                Message = message,
                LastUpdated = DateTime.UtcNow
            };

            var cacheKey = $"job_status_{jobId}";
            _memoryCache.Set(cacheKey, jobStatus, TimeSpan.FromHours(24));
            
            _logger.LogDebug("Updated job status for {JobId}: {Status} - {Message}", jobId, status, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update job status for {JobId}", jobId);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Perform memory management for long-running jobs
    /// </summary>
    private async Task PerformMemoryManagement(string jobId, int batchIndex)
    {
        try
        {
            var memoryBefore = GC.GetTotalMemory(false) / 1024 / 1024; // MB
            
            if (memoryBefore > 150) // 150MB threshold for background jobs
            {
                _logger.LogDebug("Job {JobId}: Memory cleanup at batch {BatchIndex} ({MemoryMB} MB)", 
                    jobId, batchIndex, memoryBefore);
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                _logger.LogDebug("Job {JobId}: Memory cleanup completed. {Before} MB -> {After} MB (freed {Freed} MB)",
                    jobId, memoryBefore, memoryAfter, memoryBefore - memoryAfter);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Job {JobId}: Error during memory management", jobId);
        }
        
        await Task.CompletedTask;
    }
}