using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;
using System.Text.Json;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

public class GetConnectivityAnalysisHandler : IRequestHandler<GetConnectivityAnalysisRequest, ApiResponse<ConnectivityAnalysisResponseDto>>
{
    private readonly IConnectivityAssessmentRepository _connectivityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Region> _regionRepository;
    private readonly IConnectivityCacheService _cacheService;
    private readonly ILogger<GetConnectivityAnalysisHandler> _logger;

    public GetConnectivityAnalysisHandler(
        IConnectivityAssessmentRepository connectivityRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<NTC.QoS.Domain.Entities.Province> provinceRepository,
        IRepository<NTC.QoS.Domain.Entities.Region> regionRepository,
        IConnectivityCacheService cacheService,
        ILogger<GetConnectivityAnalysisHandler> logger)
    {
        _connectivityRepository = connectivityRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ApiResponse<ConnectivityAnalysisResponseDto>> Handle(GetConnectivityAnalysisRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing connectivity analysis for {LocationCount} {LocationType}s", 
                request.LocationIds.Count, request.LocationType);

            var existingAssessments = await _connectivityRepository.GetByLocationIdsAsync(
                request.LocationIds, request.LocationType, cancellationToken);

            var locationNames = await GetLocationNamesAsync(request.LocationIds, request.LocationType, cancellationToken);

            var locations = new List<LocationConnectivityDto>();
            
            foreach (var locationId in request.LocationIds)
            {
                var assessment = existingAssessments.FirstOrDefault(a => a.LocationId == locationId);
                var locationName = locationNames.GetValueOrDefault(locationId, "Unknown");

                if (assessment != null)
                {
                    locations.Add(await MapToLocationDtoAsync(assessment, locationName, cancellationToken));
                }
                else
                {
                    // Create placeholder for locations without assessment
                    locations.Add(new LocationConnectivityDto
                    {
                        LocationId = locationId,
                        LocationName = locationName,
                        LocationType = request.LocationType,
                        ConnectivityStatus = ConnectivityStatusEnum.Unserved,
                        TotalScore = 0,
                        CMTSScores = new CMTSScoresDto(),
                        Infrastructure = new InfrastructureStatsDto(),
                        Coverage = new CoverageStatsDto(),
                        AssessmentDate = DateTime.MinValue,
                        LastUpdated = DateTime.MinValue
                    });
                }
            }

            if (request.FilterByStatus.HasValue)
            {
                locations = locations.Where(l => l.ConnectivityStatus == request.FilterByStatus.Value).ToList();
            }

            var summary = CalculateSummary(locations);

            var response = new ConnectivityAnalysisResponseDto
            {
                LocationType = request.LocationType,
                Locations = locations.OrderByDescending(l => l.TotalScore).ThenBy(l => l.LocationName).ToList(),
                Summary = summary
            };

            _logger.LogInformation("Connectivity analysis completed. Found {AssessedCount} assessed locations out of {TotalCount} requested", 
                existingAssessments.Count, request.LocationIds.Count);

            return new ApiResponse<ConnectivityAnalysisResponseDto>
            {
                Succeeded = true,
                Message = $"Connectivity analysis completed for {locations.Count} {request.LocationType}s. " +
                         $"Served: {summary.ServedCount}, Underserved: {summary.UnderservedCount}, Unserved: {summary.UnservedCount}",
                Data = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing connectivity analysis");
            return new ApiResponse<ConnectivityAnalysisResponseDto>
            {
                Succeeded = false,
                Message = "An error occurred while processing the connectivity analysis request.",
                Errors = new Dictionary<string, string[]>
                {
                    { "ProcessingError", [ex.Message] }
                }
            };
        }
    }

    private async Task<Dictionary<Guid, string>> GetLocationNamesAsync(List<Guid> locationIds, LocationTypeEnum locationType, CancellationToken cancellationToken)
    {
        return locationType switch
        {
            LocationTypeEnum.Barangay => await GetBarangayNamesAsync(locationIds, cancellationToken),
            LocationTypeEnum.Municipality => await GetMunicipalityNamesAsync(locationIds, cancellationToken),
            LocationTypeEnum.Provincial => await GetProvinceNamesAsync(locationIds, cancellationToken),
            LocationTypeEnum.Regional => await GetRegionNamesAsync(locationIds, cancellationToken),
            _ => []
        };
    }

    private async Task<Dictionary<Guid, string>> GetBarangayNamesAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var barangays = await _barangayRepository.ToListAsync(
            _barangayRepository.Query(b => locationIds.Contains(b.Id)).Select(b => new { b.Id, b.Name }), 
            cancellationToken);
        return barangays.ToDictionary(x => x.Id, x => x.Name);
    }

    private async Task<Dictionary<Guid, string>> GetMunicipalityNamesAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var municipalities = await _municipalityRepository.ToListAsync(
            _municipalityRepository.Query(m => locationIds.Contains(m.Id)).Select(m => new { m.Id, m.Name }), 
            cancellationToken);
        return municipalities.ToDictionary(x => x.Id, x => x.Name);
    }

    private async Task<Dictionary<Guid, string>> GetProvinceNamesAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var provinces = await _provinceRepository.ToListAsync(
            _provinceRepository.Query(p => locationIds.Contains(p.Id)).Select(p => new { p.Id, p.Name }), 
            cancellationToken);
        return provinces.ToDictionary(x => x.Id, x => x.Name);
    }

    private async Task<Dictionary<Guid, string>> GetRegionNamesAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var regions = await _regionRepository.ToListAsync(
            _regionRepository.Query(r => locationIds.Contains(r.Id)).Select(r => new { r.Id, r.Name }), 
            cancellationToken);
        return regions.ToDictionary(x => x.Id, x => x.Name);
    }

    private async Task<LocationConnectivityDto> MapToLocationDtoAsync(ConnectivityAssessment assessment, string locationName, CancellationToken cancellationToken)
    {
        // Retrieve technology breakdown from cache if available
        TechnologyBreakdownDto? technologyBreakdown = null;
        try
        {
            var techCacheKey = _cacheService.GenerateTechnologyCacheKey(assessment.LocationId, assessment.LocationType);
            technologyBreakdown = await _cacheService.GetCachedResultAsync<TechnologyBreakdownDto>(techCacheKey);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cache operation failed while retrieving technology breakdown for location {LocationId}", assessment.LocationId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request was cancelled while retrieving technology breakdown for location {LocationId}", assessment.LocationId);
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unexpected error retrieving technology breakdown from cache for location {LocationId}",assessment.LocationId);
        }

        // Retrieve band breakdown from cache if available
        BandBreakdownDto? bandBreakdown = null;
        try
        {
            var bandCacheKey = _cacheService.GenerateTechnologyCacheKey(assessment.LocationId, assessment.LocationType) + "_bands";
            bandBreakdown = await _cacheService.GetCachedResultAsync<BandBreakdownDto>(bandCacheKey);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cache operation failed while retrieving band breakdown for location {LocationId}", assessment.LocationId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request was cancelled while retrieving band breakdown for location {LocationId}", assessment.LocationId);
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unexpected error retrieving band breakdown from cache for location {LocationId}", assessment.LocationId);
        }

        // Retrieve technology band breakdown from cache if available
        TechnologyBandBreakdownDto? technologyBandBreakdown = null;
        try
        {
            var techBandCacheKey = _cacheService.GenerateTechnologyCacheKey(assessment.LocationId, assessment.LocationType) + "_tech_bands";
            technologyBandBreakdown = await _cacheService.GetCachedResultAsync<TechnologyBandBreakdownDto>(techBandCacheKey);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cache operation failed while retrieving technology-band breakdown for location {LocationId}", assessment.LocationId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request was cancelled while retrieving technology-band breakdown for location {LocationId}", assessment.LocationId);
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unexpected error retrieving technology-band breakdown from cache for location {LocationId}", assessment.LocationId);
        }

        // Load provider breakdown from database
        var providerBreakdown = new List<ProviderBreakdownDto>();
        try
        {
            var providers = await _connectivityRepository.GetProvidersByAssessmentIdAsync(assessment.Id, cancellationToken);
            providerBreakdown = providers.Select(p => new ProviderBreakdownDto
            {
                ProviderName = p.ProviderName,
                CellTowerCount = p.CellTowerCount,
                FiberNAPCount = p.FiberNAPCount,
                WiFiHotspotCount = p.WiFiHotspotCount,
                WirelessValidationCount = p.WirelessValidationCount,
                AssetPercentage = p.AssetPercentage
            }).ToList();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Database operation failed while retrieving provider breakdown for assessment {AssessmentId}", assessment.Id);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request was cancelled while retrieving provider breakdown for assessment {AssessmentId}", assessment.Id);
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unexpected error retrieving provider breakdown for assessment {AssessmentId}", assessment.Id);
        }

        return new LocationConnectivityDto
        {
            LocationId = assessment.LocationId,
            LocationName = locationName,
            LocationType = assessment.LocationType,
            ConnectivityStatus = assessment.ConnectivityStatus,
            TotalScore = assessment.TotalScore,
            CMTSScores = new CMTSScoresDto
            {
                FiberCoverage = assessment.FiberCoverageScore,
                InternetAccess = assessment.InternetAccessScore,
                TechnologyType = assessment.TechnologyTypeScore,
                QualityOfService = assessment.QualityOfServiceScore,
                CoverageAvailability = assessment.CoverageAvailabilityScore
            },
            Infrastructure = new InfrastructureStatsDto
            {
                CellTowerCount = assessment.CellTowerCount,
                FiberNAPCount = assessment.FiberNAPCount,
                WiFiHotspotCount = assessment.WifiHotspotCount,
                ServiceProviderCount = assessment.ServiceProviderCount,
                DominantServiceProvider = assessment.DominantServiceProvider,
                TechnologyBreakdown = technologyBreakdown,
                BandBreakdown = bandBreakdown,
                TechnologyBandBreakdown = technologyBandBreakdown,
                ProviderBreakdown = providerBreakdown
            },
            SpeedStats = assessment.SpeedTestCount > 0 ? new SpeedStatsDto
            {
                AverageDownloadSpeedMbps = assessment.AverageDownloadSpeedMbps,
                AverageUploadSpeedMbps = assessment.AverageUploadSpeedMbps,
                AverageLatencyMs = assessment.AverageLatencyMs,
                MedianDownloadSpeedMbps = assessment.MedianDownloadSpeedMbps,
                MedianUploadSpeedMbps = assessment.MedianUploadSpeedMbps,
                SpeedTestCount = assessment.SpeedTestCount,
                MeetsMinimumSpeedRequirement = assessment.MeetsMinimumSpeedRequirement,
                AverageSignalStrength = assessment.AverageSignalStrength ?? 0
            } : null,
            Coverage = new CoverageStatsDto
            {
                MobileCoveragePercentage = assessment.MobileCoveragePercentage,
                FixedBroadbandCoveragePercentage = assessment.FixedBroadbandCoveragePercentage,
                FiberCoveragePercentage = assessment.FiberCoveragePercentage,
                HasReliableConnectivity = assessment.HasReliableConnectivity,
                EstimatedPopulationCoveragePercent = assessment.EstimatedPopulationCoveragePercent,
                EstimatedHouseholdCoveragePercent = assessment.EstimatedHouseholdCoveragePercent,
                AssetsPerThousandPeople = assessment.AssetsPerThousandPeople,
                ProvidersPerThousandPeople = assessment.ProvidersPerThousandPeople
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
            Notes = assessment.Notes
        };
    }

    private static ConnectivitySummaryDto CalculateSummary(List<LocationConnectivityDto> locations)
    {
        var summary = new ConnectivitySummaryDto
        {
            TotalLocations = locations.Count,
            ServedCount = locations.Count(l => l.ConnectivityStatus == ConnectivityStatusEnum.Served),
            UnderservedCount = locations.Count(l => l.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
            UnservedCount = locations.Count(l => l.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
            TotalAreaKm2 = Math.Round(locations.Sum(l => l.AreaSizeKm2), 2),
            TotalPopulation = locations.Where(l => l.Population.HasValue).Sum(l => l.Population),
            TotalHouseholds = locations.Where(l => l.HouseholdCount.HasValue).Sum(l => l.HouseholdCount),
            AverageScore = locations.Count > 0 ? Math.Round(locations.Average(l => l.TotalScore), 2) : 0
        };

        if (locations.Count > 0)
        {
            summary.AverageCMTSScores = new CMTSScoresDto
            {
                FiberCoverage = (int)Math.Round(locations.Average(l => l.CMTSScores.FiberCoverage)),
                InternetAccess = (int)Math.Round(locations.Average(l => l.CMTSScores.InternetAccess)),
                TechnologyType = (int)Math.Round(locations.Average(l => l.CMTSScores.TechnologyType)),
                QualityOfService = (int)Math.Round(locations.Average(l => l.CMTSScores.QualityOfService)),
                CoverageAvailability = (int)Math.Round(locations.Average(l => l.CMTSScores.CoverageAvailability))
            };

            summary.TotalInfrastructure = new InfrastructureStatsDto
            {
                CellTowerCount = locations.Sum(l => l.Infrastructure.CellTowerCount),
                FiberNAPCount = locations.Sum(l => l.Infrastructure.FiberNAPCount),
                WiFiHotspotCount = locations.Sum(l => l.Infrastructure.WiFiHotspotCount),
                ServiceProviderCount = locations.Sum(l => l.Infrastructure.ServiceProviderCount)
            };
        }

        return summary;
    }
}
