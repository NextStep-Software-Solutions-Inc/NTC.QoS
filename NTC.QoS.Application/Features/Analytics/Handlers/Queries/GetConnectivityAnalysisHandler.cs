using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

public class GetConnectivityAnalysisHandler : IRequestHandler<GetConnectivityAnalysisRequest, ApiResponse<ConnectivityAnalysisResponseDto>>
{
    private readonly IConnectivityAssessmentRepository _connectivityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Region> _regionRepository;
    private readonly ILogger<GetConnectivityAnalysisHandler> _logger;

    public GetConnectivityAnalysisHandler(
        IConnectivityAssessmentRepository connectivityRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<NTC.QoS.Domain.Entities.Province> provinceRepository,
        IRepository<NTC.QoS.Domain.Entities.Region> regionRepository,
        ILogger<GetConnectivityAnalysisHandler> logger)
    {
        _connectivityRepository = connectivityRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
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
                    locations.Add(MapToLocationDto(assessment, locationName));
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
            _ => new Dictionary<Guid, string>()
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

    private static LocationConnectivityDto MapToLocationDto(ConnectivityAssessment assessment, string locationName)
    {
        // Build infrastructure stats
        var infrastructure = new InfrastructureStatsDto
        {
            CellTowerCount = assessment.CellTowerCount,
            FiberNAPCount = assessment.FiberNAPCount,
            WiFiHotspotCount = assessment.WifiHotspotCount,
            ServiceProviderCount = assessment.ServiceProviderCount,
            DominantServiceProvider = assessment.DominantServiceProvider
        };

        // Calculate coverage statistics dynamically
        var coverage = CalculateCoverageStats(
            infrastructure,
            assessment.AreaSizeKm2,
            assessment.Population,
            assessment.HouseholdCount,
            assessment.HasReliableConnectivity
        );

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
            Infrastructure = infrastructure,
            SpeedStats = assessment.SpeedTestCount > 0 ? new SpeedStatsDto
            {
                AverageDownloadSpeedMbps = assessment.AverageDownloadSpeedMbps,
                AverageUploadSpeedMbps = assessment.AverageUploadSpeedMbps,
                AverageLatencyMs = assessment.AverageLatencyMs,
                MedianDownloadSpeedMbps = assessment.MedianDownloadSpeedMbps,
                MedianUploadSpeedMbps = assessment.MedianUploadSpeedMbps,
                SpeedTestCount = assessment.SpeedTestCount,
                MeetsMinimumSpeedRequirement = assessment.MeetsMinimumSpeedRequirement
            } : null,
            Coverage = coverage,
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

    /// <summary>
    /// Calculates coverage statistics from infrastructure and population data
    /// </summary>
    private static CoverageStatsDto CalculateCoverageStats(
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
