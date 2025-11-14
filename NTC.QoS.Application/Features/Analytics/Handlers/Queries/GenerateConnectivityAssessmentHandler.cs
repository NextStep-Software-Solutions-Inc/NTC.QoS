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
    private readonly ICellSectorRepository _cellSectorRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly INAPRepository _napRepository;
    private readonly IWiFiHotspotRepository _wifiRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IWirelessValidationRepository _wirelessValidationRepository;
    private readonly IConnectivityCacheService _cacheService;
    private readonly ILogger<GenerateConnectivityAssessmentHandler> _logger;
    private readonly IUserContextService _userContextService;

    public GenerateConnectivityAssessmentHandler(
        IConnectivityAssessmentRepository connectivityRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<NTC.QoS.Domain.Entities.Province> provinceRepository,
        IRepository<NTC.QoS.Domain.Entities.Region> regionRepository,
        ICellRepository cellRepository,
        ICellSectorRepository cellSectorRepository,
        ICellularRepository cellularRepository,
        INAPRepository napRepository,
        IWiFiHotspotRepository wifiRepository,
        ISpeedtestRepository speedtestRepository,
        IWirelessValidationRepository wirelessValidationRepository,
        IConnectivityCacheService cacheService,
        ILogger<GenerateConnectivityAssessmentHandler> logger,
        IUserContextService userContextService)
    {
        _connectivityRepository = connectivityRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _cellRepository = cellRepository;
        _cellSectorRepository = cellSectorRepository;
        _cellularRepository = cellularRepository;
        _napRepository = napRepository;
        _wifiRepository = wifiRepository;
        _speedtestRepository = speedtestRepository;
        _wirelessValidationRepository = wirelessValidationRepository;
        _cacheService = cacheService;
        _logger = logger;
        _userContextService = userContextService;
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
                var assessment = await GenerateAssessmentForLocationAsync(
                    locationId, 
                    request.LocationType, 
                    request.ForceRecalculate,
                    request.StartDate, 
                    request.EndDate, 
                    cancellationToken);
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
        DateTime? startDate,
        DateTime? endDate,
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
        var infrastructureStats = await CalculateInfrastructureStatsAsync(
            boundary, center, areaKm2, locationId, locationType, 
            forceRecalculate, startDate, endDate, cancellationToken);

        // Calculate speed test statistics
        var speedStats = await CalculateSpeedTestStatsAsync(boundary, center, areaKm2, locationId, locationType, forceRecalculate, startDate, endDate, cancellationToken);

        
        // Calculate CMTS scores based on infrastructure, speed data, and population
        var cmtsScores = CalculateCMTSScores(infrastructureStats, speedStats, areaKm2, population, householdCount);

        // Determine connectivity status and total score
        var totalScore = cmtsScores.FiberCoverage + cmtsScores.InternetAccess +
                        cmtsScores.TechnologyType + cmtsScores.QualityOfService + cmtsScores.CoverageAvailability;

        var connectivityStatus = DetermineConnectivityStatus(totalScore);

        // Calculate coverage statistics to be saved
        var coverage = CalculatePopulationAwareCoverage(infrastructureStats, areaKm2, population, householdCount, 
            DetermineReliableConnectivity(infrastructureStats, speedStats));

        // Get the current user ID from context
        var currentUserId = _userContextService.GetCurrentUserId() ?? "System";

        // Create or update assessment entity
        var assessment = new ConnectivityAssessment
        {
            LocationId = locationId,
            LocationType = locationType,
            Location = center,
            AreaSizeKm2 = areaKm2,
            Population = population,
            HouseholdCount = householdCount,
            AssessmentDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            AssessedBy = currentUserId,
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

        // Save assessment
        var existingRecord = await _connectivityRepository.GetByLocationAsync(locationId, locationType, cancellationToken);
        if (existingRecord != null)
        {
            // Update existing
            existingRecord.AssessmentDate = assessment.AssessmentDate;
            existingRecord.LastUpdated = assessment.LastUpdated;
            existingRecord.Population = assessment.Population;
            existingRecord.HouseholdCount = assessment.HouseholdCount;
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
            existingRecord.AverageSignalStrength = assessment.AverageSignalStrength;
            existingRecord.MeetsMinimumSpeedRequirement = assessment.MeetsMinimumSpeedRequirement;
            existingRecord.HasReliableConnectivity = assessment.HasReliableConnectivity;
            existingRecord.MobileCoveragePercentage = assessment.MobileCoveragePercentage;
            existingRecord.FixedBroadbandCoveragePercentage = assessment.FixedBroadbandCoveragePercentage;
            existingRecord.FiberCoveragePercentage = assessment.FiberCoveragePercentage;
            existingRecord.EstimatedPopulationCoveragePercent = assessment.EstimatedPopulationCoveragePercent;
            existingRecord.EstimatedHouseholdCoveragePercent = assessment.EstimatedHouseholdCoveragePercent;
            existingRecord.AssetsPerThousandPeople = assessment.AssetsPerThousandPeople;
            existingRecord.ProvidersPerThousandPeople = assessment.ProvidersPerThousandPeople;

            _connectivityRepository.Update(existingRecord);
            
            // Update provider breakdown - delete old records and insert new ones
            await _connectivityRepository.DeleteProvidersByAssessmentIdAsync(existingRecord.Id, cancellationToken);
            
            // Batch insert all providers at once for better performance
            var providers = infrastructureStats.ProviderBreakdown
                .Select(provider => new ConnectivityAssessmentProvider
                {
                    ConnectivityAssessmentId = existingRecord.Id,
                    ProviderName = provider.ProviderName,
                    CellTowerCount = provider.CellTowerCount,
                    FiberNAPCount = provider.FiberNAPCount,
                    WiFiHotspotCount = provider.WiFiHotspotCount,
                    WirelessValidationCount = provider.WirelessValidationCount,
                    TotalAssetCount = provider.TotalAssetCount,
                    AssetPercentage = provider.AssetPercentage
                })
                .ToList();
        
            await _connectivityRepository.AddProvidersRangeAsync(providers, cancellationToken);
        }
        else
        {
            // Create new
            await _connectivityRepository.AddAsync(assessment, cancellationToken);
       
            // Save provider breakdown for new assessment - batch insert for better performance
            var providers = infrastructureStats.ProviderBreakdown
                .Select(provider => new ConnectivityAssessmentProvider
                {
                    ConnectivityAssessmentId = assessment.Id,
                    ProviderName = provider.ProviderName,
                    CellTowerCount = provider.CellTowerCount,
                    FiberNAPCount = provider.FiberNAPCount,
                    WiFiHotspotCount = provider.WiFiHotspotCount,
                    WirelessValidationCount = provider.WirelessValidationCount,
                    TotalAssetCount = provider.TotalAssetCount,
                    AssetPercentage = provider.AssetPercentage
                })
                .ToList();

            await _connectivityRepository.AddProvidersRangeAsync(providers, cancellationToken);
        }

        // Save all changes atomically in a single transaction
        await _connectivityRepository.SaveChangesAsync(cancellationToken);

        // Return DTO
        return new()
        {
            LocationId = locationId,
            LocationName = locationName,
            LocationType = locationType,
            ConnectivityStatus = connectivityStatus,
            TotalScore = totalScore,
            CMTSScores = cmtsScores,
            Infrastructure = infrastructureStats,
            SpeedStats = speedStats,
            Coverage = coverage,
            AreaSizeKm2 = areaKm2,
            Population = population,
            HouseholdCount = householdCount,
            CenterPoint = new() { Latitude = center.Y, Longitude = center.X },
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
    private async Task<InfrastructureStatsDto> CalculateInfrastructureStatsAsync(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, 
        bool forceRecalculate, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
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

        _logger.LogDebug("Calculating infrastructure stats for {LocationType} {LocationId} (Area: {Area} km�)", locationType, locationId, areaKm2);

        // Use accurate boundaries with optimizations
        var result = await CalculateInfrastructureWithBoundaryOptimization(
            boundary, center, areaKm2, locationId, locationType, 
            forceRecalculate, startDate, endDate, cancellationToken);

        // Always cache the fresh result
        var infraCacheKey = _cacheService.GenerateInfrastructureCacheKey(locationId, locationType);
        await _cacheService.SetCachedResultAsync(infraCacheKey, result);

        return result;
    }

    /// <summary>
    /// Smart boundary optimization that maintains accuracy while improving performance
    /// </summary>
    private async Task<InfrastructureStatsDto> CalculateInfrastructureWithBoundaryOptimization(
        Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, 
        bool forceRecalculate, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        List<string> cells, naps, wirelessValidations, wifiHotspots;
        TechnologyBandBreakdownDto? technologyBandBreakdown = null;

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

            // Get wireless validations using barangay center within boundary with date filter
            var wirelessValidationQuery = _wirelessValidationRepository.Query()
                .Where(wv => wv.Barangay != null
                           && wv.Barangay.Center != null
                           && boundary.Intersects(wv.Barangay.Center)
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

            // Technology-band breakdown with caching (replaces separate technology and band breakdowns)
            var techBandCacheKey = _cacheService.GenerateTechnologyCacheKey(locationId, locationType) + "_tech_band";
            technologyBandBreakdown = await _cacheService.GetCachedResultAsync<TechnologyBandBreakdownDto>(techBandCacheKey);
            if (technologyBandBreakdown == null || forceRecalculate)
            {
                if (forceRecalculate)
                {
                    _logger.LogInformation("Force recalculating technology-band breakdown for {LocationType} {LocationId}", locationType, locationId);
                }
                technologyBandBreakdown = await AnalyzeTechnologyBandBreakdownAsync(boundary, center, areaKm2, boundary != null, startDate, endDate, cancellationToken);
                await _cacheService.SetCachedResultAsync(techBandCacheKey, technologyBandBreakdown);
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

            var wirelessValidationQuery = _wirelessValidationRepository.Query()
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
            technologyBandBreakdown = await AnalyzeTechnologyBandBreakdownAsync(null, center, areaKm2, false, startDate, endDate, cancellationToken);
        }

        var allProviders = cells.Concat(naps).Concat(wifiHotspots).Concat(wirelessValidations).Where(p => !string.IsNullOrEmpty(p)).ToList();
        var uniqueProviders = allProviders.Distinct().ToList();
        var dominantProvider = allProviders.GroupBy(p => p).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;

        // Calculate provider breakdown with asset counts
        var providerBreakdown = CalculateProviderBreakdown(cells, naps, wifiHotspots, wirelessValidations);

        return new ()
        {
            CellTowerCount = cells.Count,
            FiberNAPCount = naps.Count,
            WiFiHotspotCount = wifiHotspots.Count,
            ServiceProviderCount = uniqueProviders.Count,
            DominantServiceProvider = dominantProvider,
            TechnologyBandBreakdown = technologyBandBreakdown,
            ProviderBreakdown = providerBreakdown
        };
    }

    /// <summary>
    /// Calculates detailed provider breakdown showing asset counts per provider in descending order
    /// </summary>
    private static List<ProviderBreakdownDto> CalculateProviderBreakdown(List<string> cells, List<string> naps, List<string> wifiHotspots, List<string> wirelessValidations)
    {
        // Group all assets by provider
        var providerAssets = new Dictionary<string, (int Cells, int NAPs, int WiFi, int WirelessValidations)>();

        // Count cell towers per provider
        foreach (var provider in cells.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (!providerAssets.ContainsKey(provider))
                providerAssets[provider] = (0, 0, 0, 0);
   
            var current = providerAssets[provider];
            providerAssets[provider] = (current.Cells + 1, current.NAPs, current.WiFi, current.WirelessValidations);
        }

        // Count NAPs per provider
        foreach (var provider in naps.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (!providerAssets.ContainsKey(provider))
                providerAssets[provider] = (0, 0, 0, 0);
            
            var current = providerAssets[provider];
            providerAssets[provider] = (current.Cells, current.NAPs + 1, current.WiFi, current.WirelessValidations);
        }

        // Count WiFi hotspots per provider
        foreach (var provider in wifiHotspots.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (!providerAssets.ContainsKey(provider))
                providerAssets[provider] = (0, 0, 0, 0);
            
            var current = providerAssets[provider];
            providerAssets[provider] = (current.Cells, current.NAPs, current.WiFi + 1, current.WirelessValidations);
        }

        // Count wireless validations per provider
        foreach (var provider in wirelessValidations.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (!providerAssets.ContainsKey(provider))
                providerAssets[provider] = (0, 0, 0, 0);
         
            var current = providerAssets[provider];
            providerAssets[provider] = (current.Cells, current.NAPs, current.WiFi, current.WirelessValidations + 1);
        }

        var totalAssets = cells.Count + naps.Count + wifiHotspots.Count + wirelessValidations.Count;

        // Create breakdown DTOs ordered by total asset count descending
        var breakdown = providerAssets
            .Select(kvp => new ProviderBreakdownDto
            {
                ProviderName = kvp.Key,
                CellTowerCount = kvp.Value.Cells,
                FiberNAPCount = kvp.Value.NAPs,
                WiFiHotspotCount = kvp.Value.WiFi,
                WirelessValidationCount = kvp.Value.WirelessValidations,
                AssetPercentage = totalAssets > 0 
                    ? Math.Round(((double)(kvp.Value.Cells + kvp.Value.NAPs + kvp.Value.WiFi + kvp.Value.WirelessValidations) / totalAssets) * 100, 2)
                    : 0
            })
            .OrderByDescending(p => p.TotalAssetCount)
            .ToList();

        return breakdown;
    }
  
    /// <summary>
    /// Enhanced speed stats calculation with caching
    /// </summary>
    private async Task<SpeedStatsDto?> CalculateSpeedTestStatsAsync(Geometry? boundary, Point center, double areaKm2, Guid locationId, LocationTypeEnum locationType, bool forceRecalculate, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
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

            var query = _speedtestRepository.Query()
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
    /// Analyzes band frequency distribution from cellular deployments in the area
    /// OPTIMIZED: Uses database-side aggregation with single query and projection
    /// </summary>
    private async Task<BandBreakdownDto> AnalyzeBandFrequencyAsync(Geometry? boundary, Point center, double areaKm2, bool useBoundary, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            // Dictionary to accumulate band counts from both sources
            var bandCounts = new Dictionary<string, int>();

            if (useBoundary && boundary != null)
            {
                // OPTIMIZED: Single query with joins - let database do the work
                var cellQuery = _cellRepository.Query();
                cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);

                // Get band data from Cellular using single query with joins
                var cellularBandData = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => c.CellSector != null 
                                 && c.CellSector.Cell != null
                                 && cellQuery.Select(cell => cell.Id).Contains(c.CellSector.Cell.Id)
                                 && c.Band != null)
                        .Select(c => c.Band!.Name), cancellationToken);

                // Aggregate cellular band data
                foreach (var bandName in cellularBandData.Where(b => !string.IsNullOrEmpty(b)))
                {
                    var normalizedBand = NormalizeBandName(bandName);
                    if (!string.IsNullOrEmpty(normalizedBand))
                    {
                        bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                    }
                }

                // Get wireless validation band data with date filtering
                var wirelessValidationQuery = _wirelessValidationRepository.Query()
                    .Where(wv => wv.Barangay != null 
                               && wv.Barangay.Center != null
                               && boundary.Intersects(wv.Barangay.Center)
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
                foreach (var bandName in wirelessValidationBandData.Where(b => !string.IsNullOrEmpty(b)))
                {
                    var normalizedBand = NormalizeBandName(bandName);
                    if (!string.IsNullOrEmpty(normalizedBand))
                    {
                        bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                    }
                }
            }
            else
            {
                // Use radius-based queries for larger areas (AnalyzeBandFrequencyAsync - Handler)
                var searchRadiusKm = Math.Min(Math.Sqrt(areaKm2 / Math.PI), 50);
                var searchRadiusMeters = searchRadiusKm * 1000;

                // OPTIMIZED: Single query with distance filter
                var cellularBandData = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => c.CellSector!.Cell.Location.Distance(center) <= searchRadiusMeters
                                 && c.Band != null)
                        .Select(c => c.Band!.Name), cancellationToken);

                // Aggregate cellular band data
                foreach (var bandName in cellularBandData.Where(b => !string.IsNullOrEmpty(b)))
                {
                    var normalizedBand = NormalizeBandName(bandName);
                    if (!string.IsNullOrEmpty(normalizedBand))
                    {
                        bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                    }
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
                foreach (var bandName in wirelessValidationBandData.Where(b => !string.IsNullOrEmpty(b)))
                {
                    var normalizedBand = NormalizeBandName(bandName);
                    if (!string.IsNullOrEmpty(normalizedBand))
                    {
                        bandCounts[normalizedBand] = bandCounts.GetValueOrDefault(normalizedBand, 0) + 1;
                    }
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
    /// Analyzes technology-specific band frequency distribution from cellular deployments
    /// Returns deployment counts for each frequency band broken down by technology generation (5G, LTE, 3G, 2G)
    /// OPTIMIZED: Uses database-side aggregation with single query and joins
    /// </summary>
    private async Task<TechnologyBandBreakdownDto> AnalyzeTechnologyBandBreakdownAsync(Geometry? boundary, Point center, double areaKm2, bool useBoundary, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            // Dictionaries to track both unique bands per technology and deployment counts
            var techBandSets = new Dictionary<string, HashSet<string>>();
            var technologyCounts = new Dictionary<string, int>();
            var generationCounts = new Dictionary<string, int>();

            if (useBoundary && boundary != null)
            {
                // OPTIMIZED: Single query with joins - let database do the work
                var cellQuery = _cellRepository.Query();
                cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);

                // Get technology-band-generation data from Cellular using single query with joins
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

                // Collect unique bands and counts for each technology
                foreach (var item in cellularTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Track technology and generation counts
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    generationCounts[item.GenerationName] = generationCounts.GetValueOrDefault(item.GenerationName, 0) + 1;
                }

                // Get wireless validation technology-band-generation data with date filtering
                var wirelessValidationQuery = _wirelessValidationRepository.Query()
                    .Where(wv => wv.Barangay != null 
                               && wv.Barangay.Center != null
                               && boundary.Intersects(wv.Barangay.Center)
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

                // Collect unique bands and counts for each technology from wireless validations
                foreach (var item in wirelessValidationTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Track technology and generation counts
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    generationCounts[item.GenerationName] = generationCounts.GetValueOrDefault(item.GenerationName, 0) + 1;
                }
            }
            else
            {
                // Use radius-based queries for larger areas (AnalyzeTechnologyBandBreakdownAsync - Handler)
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

                // Collect unique bands and counts for each technology
                foreach (var item in cellularTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Track technology and generation counts
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    generationCounts[item.GenerationName] = generationCounts.GetValueOrDefault(item.GenerationName, 0) + 1;
                }

                // Get wireless validation technology-band-generation data using barangay center distance
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

                // Collect unique bands and counts for each technology from wireless validations
                foreach (var item in wirelessValidationTechBandData.Where(b => !string.IsNullOrEmpty(b.BandName) && !string.IsNullOrEmpty(b.TechnologyStandard)))
                {
                    if (!techBandSets.ContainsKey(item.TechnologyStandard))
                    {
                        techBandSets[item.TechnologyStandard] = new HashSet<string>();
                    }
                    techBandSets[item.TechnologyStandard].Add(item.BandName);
                    
                    // Track technology and generation counts
                    technologyCounts[item.TechnologyStandard] = technologyCounts.GetValueOrDefault(item.TechnologyStandard, 0) + 1;
                    generationCounts[item.GenerationName] = generationCounts.GetValueOrDefault(item.GenerationName, 0) + 1;
                }
            }

            if (!techBandSets.Any() || techBandSets.All(kvp => kvp.Value.Count == 0))
            {
                return new TechnologyBandBreakdownDto();
            }

            // Build the DTO with both technology-band pairs and statistics
            var result = new TechnologyBandBreakdownDto();
            
            // Add technology-band pairs
            foreach (var kvp in techBandSets.Where(x => x.Value.Any()))
            {
                result[kvp.Key] = kvp.Value.OrderBy(b => b).ToList();
            }
            
            // Populate generation statistics
            result.AvailableGenerations = generationCounts.Keys.OrderBy(g => g).ToList();
            result.Count2G = generationCounts.GetValueOrDefault("2G", 0);
            result.Count3G = generationCounts.GetValueOrDefault("3G", 0);
            result.Count4G = generationCounts.GetValueOrDefault("4G", 0);
            result.Count5G = generationCounts.GetValueOrDefault("5G", 0);
            
            // Calculate dominant generation and primary technology
            if (generationCounts.Any())
            {
                result.DominantGeneration = generationCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            }
            
            if (technologyCounts.Any())
            {
                result.PrimaryTechnology = technologyCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            }
            
            // Calculate modern coverage statistics
            var totalCount = technologyCounts.Values.Sum();
            result.TotalTechnologyCount = totalCount;
            var modernCount = result.Count4G + result.Count5G;
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
    /// Normalizes generation names to standard format (5G, LTE, 3G, 2G)
    /// </summary>
    private static string NormalizeGenerationName(string generationName)
    {
        if (string.IsNullOrEmpty(generationName))
            return string.Empty;

        var normalized = generationName.ToUpperInvariant().Trim();

        return normalized switch
        {
            var s when s.Contains("5G") || s.Contains("NR") || s.Contains("FIFTH") => "5G",
            var s when s.Contains("4G") || s.Contains("LTE") || s.Contains("FOURTH") => "LTE",
            var s when s.Contains("3G") || s.Contains("UMTS") || s.Contains("WCDMA") || s.Contains("HSPA") || s.Contains("THIRD") => "3G",
            var s when s.Contains("2G") || s.Contains("GSM") || s.Contains("EDGE") || s.Contains("GPRS") || s.Contains("SECOND") => "2G",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Analyzes technology generations in the area from speedtest data and cellular infrastructure
    /// </summary>
    private async Task<TechnologyBreakdownDto> AnalyzeTechnologyGenerationsAsync(Geometry? boundary, Point center, double areaKm2, bool useBoundary, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            var allTechData = new List<(string GenerationName, string TechnologyStandard, Guid Id)>();

            if (useBoundary && boundary != null)
            {
                // Use boundary-based queries for speedtest data
                var speedtestQuery = _speedtestRepository.Query();
                speedtestQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, speedtestQuery);
                
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
                        .Select(s => new {
                            GenerationName = s.Generation!.Name,
                            TechnologyStandard = s.Technology!.Standard,
                            s.Id
                        }), cancellationToken);

                allTechData.AddRange(speedtestResults.Select(s => (s.GenerationName, s.TechnologyStandard, s.Id)));

                // Get cellular technology data using cell repository with spatial filter
                // First get the cell IDs within the boundary
                var cellQuery = _cellRepository.Query();
                cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(boundary, cellQuery);
                var cellIds = await _cellRepository.ToListAsync(
                    cellQuery.Select(c => c.Id), cancellationToken);

                // Get cell sector IDs from those cells using CellSector repository
                var cellSectorIds = await _cellSectorRepository.ToListAsync(
                    _cellSectorRepository.Query()
                        .Where(cs => cellIds.Contains(cs.CellId))
                        .Select(cs => cs.Id), cancellationToken);

                // Query cellulars using the cell sector IDs (direct relationship)
                var cellularResults = await _cellularRepository.ToListAsync(
                    _cellularRepository.Query()
                        .Where(c => cellSectorIds.Contains(c.CellSectorId)
                                 && c.Technology != null 
                                 && c.Technology.Generation != null)
                        .Select(c => new {
                            GenerationName = c.Technology!.Generation!.Name,
                            TechnologyStandard = c.Technology!.Standard,
                            c.Id
                        }), cancellationToken);

                allTechData.AddRange(cellularResults.Select(c => (c.GenerationName, c.TechnologyStandard, c.Id)));

                // Get wireless validation technology data
                var wirelessValidationQuery = _wirelessValidationRepository.Query()
                    .Where(wv => wv.Barangay != null 
                               && wv.Barangay.Center != null
                               && boundary.Intersects(wv.Barangay.Center)
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
                    wirelessValidationQuery.Select(wv => new {
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

                var speedtestQuery = _speedtestRepository.Query()
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
                    speedtestQuery.Select(s => new {
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
                var wirelessValidationQuery = _wirelessValidationRepository.Query()
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
                    wirelessValidationQuery.Select(wv => new {
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
            _ => ConnectivityStatusEnum.Unserved        // =5 points
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
    /// </summary>
    private static int CalculateTechnologyTypeScore(InfrastructureStatsDto infrastructure)
    {
        var hasModernInfra = infrastructure.FiberNAPCount > 0 || infrastructure.CellTowerCount > 0;
        var hasDiverseInfra = (infrastructure.FiberNAPCount > 0 ? 1 : 0) +
                             (infrastructure.CellTowerCount > 0 ? 1 : 0) +
                             (infrastructure.WiFiHotspotCount > 0 ? 1 : 0);

        // Enhanced technology scoring using actual generation data
        if (infrastructure.TechnologyBandBreakdown != null && infrastructure.TechnologyBandBreakdown.TotalTechnologyCount > 0)
        {
            var tech = infrastructure.TechnologyBandBreakdown;

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
        if (infrastructure.TechnologyBandBreakdown?.Count5G > 0 &&
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
    /// Population-aware scoring aligns with CMTS rubric requirements
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
            var hasModernTech = infrastructure.TechnologyBandBreakdown?.HasModernTechnology == true;

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
    /// Calculates average signal strength based on RSSI values and signal quality strings
    /// Uses the SignalStrengthHelper from Domain.Helper for consistent signal processing
    /// </summary>
    private static double CalculateAverageSignalStrength(List<decimal> rssiValues, List<string> signalQualities)
    {
        return SignalStrengthHelper.CalculateAverageSignalStrength(rssiValues, signalQualities);
    }
}