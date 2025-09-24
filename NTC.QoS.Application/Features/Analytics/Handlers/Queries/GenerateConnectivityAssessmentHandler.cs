using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

public class GenerateConnectivityAssessmentHandler : IRequestHandler<GenerateConnectivityAssessmentRequest, ApiResponse<List<LocationConnectivityDto>>>
{
    private readonly IConnectivityAssessmentRepository _connectivityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Region> _regionRepository;
    private readonly ICellRepository _cellRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly INAPRepository _napRepository;
    private readonly IWiFiHotspotRepository _wifiRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IConnectivityCacheService _cacheService;
    private readonly ILogger<GenerateConnectivityAssessmentHandler> _logger;

    public GenerateConnectivityAssessmentHandler(
        IConnectivityAssessmentRepository connectivityRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<NTC.QoS.Domain.Entities.Province> provinceRepository,
        IRepository<NTC.QoS.Domain.Entities.Region> regionRepository,
        ICellRepository cellRepository,
        ICellularRepository cellularRepository,
        INAPRepository napRepository,
        IWiFiHotspotRepository wifiRepository,
        ISpeedtestRepository speedtestRepository,
        IConnectivityCacheService cacheService,
        ILogger<GenerateConnectivityAssessmentHandler> logger)
    {
        _connectivityRepository = connectivityRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _cellRepository = cellRepository;
        _cellularRepository = cellularRepository;
        _napRepository = napRepository;
        _wifiRepository = wifiRepository;
        _speedtestRepository = speedtestRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<LocationConnectivityDto>>> Handle(GenerateConnectivityAssessmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating connectivity assessments for {LocationCount} {LocationType}s", 
                request.LocationIds.Count, request.LocationType);

            var generatedAssessments = new List<LocationConnectivityDto>();
            
            foreach (var locationId in request.LocationIds)
            {
                var assessment = await GenerateAssessmentForLocationAsync(locationId, request.LocationType, request.ForceRecalculate, cancellationToken);
                if (assessment != null)
                {
                    generatedAssessments.Add(assessment);
                }
            }

            _logger.LogInformation("Generated {AssessmentCount} connectivity assessments", generatedAssessments.Count);

            return new ApiResponse<List<LocationConnectivityDto>>
            {
                Succeeded = true,
                Message = $"Successfully generated {generatedAssessments.Count} connectivity assessments",
                Data = generatedAssessments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating connectivity assessments");
            return new ApiResponse<List<LocationConnectivityDto>>
            {
                Succeeded = false,
                Message = "An error occurred while generating connectivity assessments.",
                Errors = new Dictionary<string, string[]>
                {
                    { "GenerationError", [ex.Message] }
                }
            };
        }
    }

    private async Task<LocationConnectivityDto?> GenerateAssessmentForLocationAsync(
        Guid locationId, 
        LocationTypeEnum locationType, 
        bool forceRecalculate, 
        CancellationToken cancellationToken)
    {
        // Check if assessment already exists
        if (!forceRecalculate)
        {
            var existingAssessment = await _connectivityRepository.GetByLocationAsync(locationId, locationType, cancellationToken);
            if (existingAssessment != null && existingAssessment.LastUpdated > DateTime.UtcNow.AddDays(-30))
            {
                _logger.LogInformation("Using existing assessment for location {LocationId} (last updated: {LastUpdated})", 
                    locationId, existingAssessment.LastUpdated);
                return null; // Skip generation, use existing
            }
        }

        // Get location information and boundaries
        var (locationName, boundary, center, areaKm2, population, householdCount) = await GetLocationDetailsAsync(locationId, locationType, cancellationToken);
        if (center == null)
        {
            _logger.LogWarning("Could not get location information for location {LocationId}", locationId);
            return null;
        }

        // Calculate infrastructure counts within the boundary
        var infrastructureStats = await CalculateInfrastructureStatsAsync(boundary, center, areaKm2, locationId, locationType, forceRecalculate, cancellationToken);
        
        // Calculate speed test statistics
        var speedStats = await CalculateSpeedTestStatsAsync(boundary, center, areaKm2, locationId, locationType, forceRecalculate, cancellationToken);
        
        // Calculate CMTS scores based on infrastructure, speed data, and population (NEW!)
        var cmtsScores = CalculateCMTSScores(infrastructureStats, speedStats, areaKm2, population, householdCount);
        
        // Determine connectivity status and total score
        var totalScore = cmtsScores.FiberCoverage + cmtsScores.InternetAccess + 
                        cmtsScores.TechnologyType + cmtsScores.QualityOfService + cmtsScores.CoverageAvailability;
        
        var connectivityStatus = DetermineConnectivityStatus(totalScore);

        // Create or update assessment entity
        var assessment = new ConnectivityAssessment
        {
            LocationId = locationId,
            LocationType = locationType,
            Location = center,
            AreaSizeKm2 = areaKm2,
            Population = population,               // NEW: Store population data
            HouseholdCount = householdCount,       // NEW: Store household count
            AssessmentDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            AssessedBy = "System",
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
            MeetsMinimumSpeedRequirement = speedStats?.MeetsMinimumSpeedRequirement ?? false,
            HasReliableConnectivity = DetermineReliableConnectivity(infrastructureStats, speedStats)
        };

        // Save assessment
        var existingRecord = await _connectivityRepository.GetByLocationAsync(locationId, locationType, cancellationToken);
        if (existingRecord != null)
        {
            // Update existing
            existingRecord.AssessmentDate = assessment.AssessmentDate;
            existingRecord.LastUpdated = assessment.LastUpdated;
            existingRecord.Population = assessment.Population;                    // NEW: Update population
            existingRecord.HouseholdCount = assessment.HouseholdCount;            // NEW: Update household count
            existingRecord.ConnectivityStatus = assessment.ConnectivityStatus;
            existingRecord.TotalScore = assessment.TotalScore;
            existingRecord.FiberCoverageScore = assessment.FiberCoverageScore;
            existingRecord.InternetAccessScore = assessment.InternetAccessScore;
            existingRecord.TechnologyTypeScore = assessment.TechnologyTypeScore;
            existingRecord.QualityOfServiceScore = assessment.QualityOfServiceScore;
            existingRecord.CoverageAvailabilityScore = assessment.CoverageAvailabilityScore;
            existingRecord.CellTowerCount = assessment.CellTowerCount;
            existingRecord.FiberNAPCount = assessment.FiberNAPCount;
            existingRecord.WifiHotspotCount = assessment.WifiHotspotCount;
            existingRecord.ServiceProviderCount = assessment.ServiceProviderCount;
            existingRecord.DominantServiceProvider = assessment.DominantServiceProvider;
            existingRecord.AverageDownloadSpeedMbps = assessment.AverageDownloadSpeedMbps;
            existingRecord.AverageUploadSpeedMbps = assessment.AverageUploadSpeedMbps;
            existingRecord.AverageLatencyMs = assessment.AverageLatencyMs;
            existingRecord.MedianDownloadSpeedMbps = assessment.MedianDownloadSpeedMbps;
            existingRecord.MedianUploadSpeedMbps = assessment.MedianUploadSpeedMbps;
            existingRecord.SpeedTestCount = assessment.SpeedTestCount;
            existingRecord.MeetsMinimumSpeedRequirement = assessment.MeetsMinimumSpeedRequirement;
            existingRecord.HasReliableConnectivity = assessment.HasReliableConnectivity;

            _connectivityRepository.Update(existingRecord);
        }
        else
        {
            // Create new
            await _connectivityRepository.AddAsync(assessment, cancellationToken);
        }
        
        await _connectivityRepository.SaveChangesAsync(cancellationToken);

        // Return DTO
        return new LocationConnectivityDto
        {
            LocationId = locationId,
            LocationName = locationName,
            LocationType = locationType,
            ConnectivityStatus = connectivityStatus,
            TotalScore = totalScore,
            CMTSScores = cmtsScores,
            Infrastructure = infrastructureStats,
            SpeedStats = speedStats,
            Coverage = CalculatePopulationAwareCoverage(infrastructureStats, areaKm2, population, householdCount, assessment.HasReliableConnectivity),
            AreaSizeKm2 = areaKm2,
            Population = population,                                    // NEW: Include population data
            HouseholdCount = householdCount,                           // NEW: Include household count  
            CenterPoint = new PointDto { Latitude = center.Y, Longitude = center.X },
            AssessmentDate = assessment.AssessmentDate,
            LastUpdated = assessment.LastUpdated
        };
    }

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
    private async Task<InfrastructureStatsDto> CalculateInfrastructureStatsAsync(Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, CancellationToken cancellationToken)
    {
        // Only check cache if not forcing recalculation
        if (!forceRecalculate)
        {
            var cacheKey = _cacheService.GenerateInfrastructureCacheKey(locationId, locationType);
            var cachedResult = await _cacheService.GetCachedResultAsync<InfrastructureStatsDto>(cacheKey);
            if (cachedResult != null)
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
        var infraCacheKey = _cacheService.GenerateInfrastructureCacheKey(locationId, locationType);
        await _cacheService.SetCachedResultAsync(infraCacheKey, result);
        
        return result;
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
            // **ALWAYS use accurate boundaries when available** - this is crucial for government data
            _logger.LogDebug("Using accurate MultiPolygon boundaries for precise infrastructure counting");

            // Get cells with envelope optimization
            var cellQuery = _cellRepository.Query();
            cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);
            cells = await _cellRepository.ToListAsync(
                cellQuery.Select(c => c.ServiceProvider.Provider.Name), cancellationToken);

            // Get NAPs with envelope optimization
            var napQuery = _napRepository.Query();
            napQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, napQuery);
            naps = await _napRepository.ToListAsync(
                napQuery.Select(n => n.ServiceProvider.Provider.Name), cancellationToken);

            // Get WiFi hotspots with envelope optimization
            var wifiQuery = _wifiRepository.Query();
            wifiQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, wifiQuery);
            wifiHotspots = await _wifiRepository.ToListAsync(
                wifiQuery.Select(w => w.ServiceProvider.Provider.Name), cancellationToken);

            // Get technology breakdown with caching
            var techCacheKey = _cacheService.GenerateTechnologyCacheKey(locationId, locationType);
            technologyBreakdown = await _cacheService.GetCachedResultAsync<TechnologyBreakdownDto>(techCacheKey);
            if (technologyBreakdown == null || forceRecalculate)
            {
                if (forceRecalculate)
                {
                    _logger.LogInformation("Force recalculating technology breakdown for {LocationType} {LocationId}", locationType, locationId);
                }
                technologyBreakdown = await AnalyzeTechnologyGenerationsAsync(boundary, center, areaKm2, true, cancellationToken);
                await _cacheService.SetCachedResultAsync(techCacheKey, technologyBreakdown);
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

        return new InfrastructureStatsDto
        {
            CellTowerCount = cells.Count,
            FiberNAPCount = naps.Count,
            WiFiHotspotCount = wifiHotspots.Count,
            ServiceProviderCount = uniqueProviders.Count,
            DominantServiceProvider = dominantProvider,
            TechnologyBreakdown = technologyBreakdown
        };
    }

    /// <summary>
    /// Enhanced speed stats calculation with caching
    /// </summary>
    private async Task<SpeedStatsDto?> CalculateSpeedTestStatsAsync(Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, CancellationToken cancellationToken)
    {
        // Only check cache if not forcing recalculation
        if (!forceRecalculate)
        {
            var cacheKey = _cacheService.GenerateSpeedStatsCacheKey(locationId, locationType);
            var cachedResult = await _cacheService.GetCachedResultAsync<SpeedStatsDto>(cacheKey);
            if (cachedResult != null)
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
            var speedCacheKey = _cacheService.GenerateSpeedStatsCacheKey(locationId, locationType);
            await _cacheService.SetCachedResultAsync(speedCacheKey, result);
        }

        return result;
    }

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
        
        // Determine average signal strength based on RSSI ranges
        var averageSignalStrength = CalculateAverageSignalStrength(rssiValues, signalQualities);
        
        return new SpeedStatsDto
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
            }

            if (allTechData.Count == 0)
            {
                return new TechnologyBreakdownDto();
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

            return new TechnologyBreakdownDto
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
            return new TechnologyBreakdownDto();
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

    private static decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0) return 0;
        
        int mid = values.Count / 2;
        return values.Count % 2 == 0 
            ? Math.Round((values[mid - 1] + values[mid]) / 2, 2)
            : Math.Round(values[mid], 2);
    }

    private static ConnectivityStatusEnum DetermineConnectivityStatus(int totalScore)
    {
        return totalScore switch
        {
            >= 11 => ConnectivityStatusEnum.Served,    // 11-15 points
            >= 6 => ConnectivityStatusEnum.Underserved, // 6-10 points  
            _ => ConnectivityStatusEnum.Unserved        // ≤5 points
        };
    }

    private static bool DetermineReliableConnectivity(InfrastructureStatsDto infrastructure, SpeedStatsDto? speedStats)
    {
        var hasInfrastructure = infrastructure.TotalAssets > 0;
        var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;
        var hasGoodSpeeds = speedStats?.MeetsMinimumSpeedRequirement == true;

        return hasInfrastructure && (hasMultipleProviders || hasGoodSpeeds);
    }

    private static CMTSScoresDto CalculateCMTSScores(InfrastructureStatsDto infrastructure, SpeedStatsDto? speedStats, double areaKm2, int? population, int? householdCount)
    {
        var scores = new CMTSScoresDto();

        // Calculate population density for context
        var populationDensity = population.HasValue && areaKm2 > 0 ? population.Value / areaKm2 : 0;
        var isHighDensityArea = populationDensity > 500; // >500 people per km² = high density
        var isUrbanArea = populationDensity > 1000; // >1000 people per km² = urban
        
        // 1. Fiber Coverage (enhanced with population context)
        if (infrastructure.FiberNAPCount == 0)
        {
            scores.FiberCoverage = 1; // Unserved - No installed or present FTTH facilities
        }
        else
        {
            // Population-aware fiber coverage calculation
            double fiberDensityScore;
            
            if (population.HasValue && population > 0)
            {
                // Calculate fiber NAPs per 1000 people
                var fiberPer1000People = (infrastructure.FiberNAPCount * 1000.0) / population.Value;
                
                if (isUrbanArea)
                {
                    // Urban areas need higher fiber density
                    fiberDensityScore = fiberPer1000People >= 2.0 ? 3 :    // 2+ NAPs per 1000 people = Served
                                       fiberPer1000People >= 0.5 ? 2 :     // 0.5-2 NAPs per 1000 people = Underserved
                                       1;                                   // <0.5 NAPs per 1000 people = Unserved
                }
                else if (isHighDensityArea)
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
            
            scores.FiberCoverage = (int)Math.Round(fiberDensityScore);
        }

        // 2. Internet Access (based on speed test data)
        if (speedStats == null || speedStats.SpeedTestCount == 0)
        {
            scores.InternetAccess = 1; // No data - assume unserved
        }
        else if (speedStats.AverageDownloadSpeedMbps < 25)
        {
            scores.InternetAccess = 1; // Unserved - No internet access or minimal
        }
        else if (speedStats.AverageDownloadSpeedMbps >= 25 && speedStats.AverageDownloadSpeedMbps < 100)
        {
            scores.InternetAccess = 2; // Underserved - Available but slow/unreliable
        }
        else
        {
            scores.InternetAccess = 3; // Served - Stable internet meeting/exceeding standards
        }

        // 3. Technology Type (enhanced with generation mapping and technology analysis)
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
                scores.TechnologyType = 3; // Served - 5G/LTE infrastructure active
            }
            else if (tech.Count4G > 0 && !tech.IsLegacyDominated) // 4G present and not legacy-dominated
            {
                scores.TechnologyType = 3; // Served - 4G/LTE infrastructure active  
            }
            else if (tech.HasModernTechnology && tech.IsLegacyDominated) // Has 4G/5G but legacy dominates
            {
                scores.TechnologyType = 2; // Underserved - Mixed legacy and modern
            }
            else if (tech.Count3G > 0 || tech.Count2G > 0) // Only legacy technologies
            {
                scores.TechnologyType = 2; // Underserved - Legacy technologies only
            }
            else
            {
                scores.TechnologyType = 1; // Unserved - No identifiable technology
            }
        }
        else
        {
            // Fallback to infrastructure-based scoring when no technology data available
            if (!hasModernInfra)
            {
                scores.TechnologyType = 1; // Unserved - No installed broadband technology
            }
            else if (hasDiverseInfra == 1)
            {
                scores.TechnologyType = 2; // Underserved - Limited infrastructure diversity
            }
            else
            {
                scores.TechnologyType = 3; // Served - Diverse infrastructure present
            }
        }

        // 4. Quality of Service (enhanced with RSSI data from speedtests)
        if (speedStats == null || speedStats.SpeedTestCount < 5) // Insufficient data
        {
            scores.QualityOfService = 1; // Assume poor quality without data
        }
        else
        {
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
            
            scores.QualityOfService = qualityFactors switch
            {
                >= 3 => 3, // Served - High quality (good speed, low latency, good signal)
                2 => 2,    // Underserved - Fair quality but some issues
                _ => 1     // Unserved - Poor quality
            };
        }

        // 5. Overall Coverage Availability (POPULATION-AWARE - This is the key CMTS requirement!)
        if (infrastructure.TotalAssets == 0)
        {
            scores.CoverageAvailability = 1; // Unserved - No coverage
        }
        else
        {
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
                
                if (isUrbanArea)
                {
                    // Urban: High infrastructure density expected  
                    estimatedCoveragePercent = Math.Min(100, (assetsPerCapita * 10000) + (providersPerThousand * 20));
                }
                else if (isHighDensityArea)
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
            
            scores.CoverageAvailability = (int)Math.Round(coverageScore);
        }

        return scores;
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
            
            // Estimate household coverage (assuming similar ratio)
            if (householdCount.HasValue && householdCount > 0)
            {
                var householdRatio = (double)householdCount.Value / population.Value;
                coverage.EstimatedHouseholdCoveragePercent = Math.Round(estimatedCoveragePercent * householdRatio, 2);
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
    /// Calculates average signal strength based on RSSI values and signal quality strings
    /// Uses the SignalStrengthHelper from Domain.Helper for consistent signal processing
    /// </summary>
    private static double CalculateAverageSignalStrength(List<decimal> rssiValues, List<string> signalQualities)
    {
        return SignalStrengthHelper.CalculateAverageSignalStrength(rssiValues, signalQualities);
    }
}
