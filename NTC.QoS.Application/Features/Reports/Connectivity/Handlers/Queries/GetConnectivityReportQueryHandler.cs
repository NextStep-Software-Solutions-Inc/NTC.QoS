using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.Connectivity.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;
using System.Collections.Concurrent;

namespace NTC.QoS.Application.Features.Reports.Connectivity.Handlers.Queries;

public class GetConnectivityReportQueryHandler : IRequestHandler<GetConnectivityReportQuery, ApiResponse<ConnectivityReportDto>>
{
    private readonly IConnectivityAssessmentRepository _assessmentRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository;
    private readonly ILogger<GetConnectivityReportQueryHandler> _logger;

    // Cache for location names during the request lifetime
    private readonly ConcurrentDictionary<(Guid, LocationTypeEnum), string> _locationNameCache = new();

    public GetConnectivityReportQueryHandler(
        IConnectivityAssessmentRepository assessmentRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IProvinceRepository provinceRepository,
        IRegionRepository regionRepository,
        IBarangayCoverageRepository barangayCoverageRepository,
        ILogger<GetConnectivityReportQueryHandler> logger)
    {
        _assessmentRepository = assessmentRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _barangayCoverageRepository = barangayCoverageRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<ConnectivityReportDto>> Handle(GetConnectivityReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating unified connectivity report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, FilterByStatus={FilterByStatus}, PageNumber={PageNumber}, PageSize={PageSize}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, request.FilterByStatus, request.PageNumber, request.PageSize);

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return new ApiResponse<ConnectivityReportDto>
                {
                    Succeeded = false,
                    Message = "PageNumber must be at least 1.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "PageNumber", ["PageNumber must be at least 1."] }
                    }
                };
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return new ApiResponse<ConnectivityReportDto>
                {
                    Succeeded = false,
                    Message = "PageSize must be between 1 and 100.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "PageSize", ["PageSize must be between 1 and 100."] }
                    }
                };
            }

            // Fetch all assessments for the specified geographic area
            var assessments = await GetFilteredAssessmentsAsync(
                request.RegionId, request.ProvinceId, request.MunicipalityId, LocationTypeEnum.Barangay, cancellationToken);

            // Apply optional status filter; by default include all statuses (unified view)
            var filteredAreas = request.FilterByStatus.HasValue
                ? assessments.Where(a => a.ConnectivityStatus == request.FilterByStatus.Value).ToList()
                : assessments;

            // Order by connectivity status priority (unserved first), then by population descending
            var orderedAreas = filteredAreas
                .OrderBy(a => a.ConnectivityStatus)
                .ThenByDescending(a => a.Population ?? 0)
                .ThenByDescending(a => a.TotalScore)
                .ToList();

            var totalAreas = orderedAreas.Count;
            var totalPages = totalAreas > 0 ? (int)Math.Ceiling(totalAreas / (double)request.PageSize) : 0;
            var skip = (request.PageNumber - 1) * request.PageSize;
            var paginatedAreas = orderedAreas.Skip(skip).Take(request.PageSize).ToList();

            // OPTIMIZED: Batch load location names for paginated results only
            var locationNames = await GetLocationNamesBatchAsync(
                paginatedAreas.Select(a => (a.LocationId, a.LocationType)).ToList(),
                cancellationToken);

            // When summaries are requested, fetch barangay details for ALL assessments so geographic
            // summaries (municipality/province/overall) cover the full unfiltered scope.
            // When summaries are not needed (e.g., subsequent pagination pages), only fetch details
            // for the paginated results to avoid a potentially large IN query across all assessments.
            var allBarangayDetails = await GetBarangayDetailsAsync(
                request.IncludeSummaries
                    ? assessments.Select(a => a.LocationId).ToList()
                    : paginatedAreas.Select(a => a.LocationId).ToList(),
                cancellationToken);

            var areaDetails = paginatedAreas.Select(a => new ConnectivityAreaDetailDto
            {
                LocationId = a.LocationId,
                LocationName = locationNames.GetValueOrDefault((a.LocationId, a.LocationType), $"Location-{a.LocationId}"),
                MunicipalityName = allBarangayDetails.GetValueOrDefault(a.LocationId).MunicipalityName,
                ProvinceName = allBarangayDetails.GetValueOrDefault(a.LocationId).ProvinceName,
                ConnectivityStatus = a.ConnectivityStatus,
                Population = a.Population,
                FiberCoverageScore = a.FiberCoverageScore,
                InternetAccessScore = a.InternetAccessScore,
                TechnologyTypeScore = a.TechnologyTypeScore,
                QualityOfServiceScore = a.QualityOfServiceScore,
                CoverageAvailabilityScore = a.CoverageAvailabilityScore,
                TotalScore = a.TotalScore,
                CellTowerCount = a.CellTowerCount,
                FiberNAPCount = a.FiberNAPCount,
                WifiHotspotCount = a.WifiHotspotCount,
                ServiceProviderCount = a.ServiceProviderCount,
                DominantServiceProvider = a.DominantServiceProvider,
                AverageDownloadSpeedMbps = a.AverageDownloadSpeedMbps,
                AverageUploadSpeedMbps = a.AverageUploadSpeedMbps,
                SpeedTestCount = a.SpeedTestCount,
                LastAssessed = a.AssessmentDate
            }).ToList();

            // Calculate summary statistics using all assessments (not just the current page)
            // Always compute from the full unfiltered set so the summary reflects the whole dataset
            var servedAreas = assessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served).ToList();
            var underservedAreas = assessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved).ToList();
            var unservedAreas = assessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved).ToList();

            // Calculate summary statistics from the full unfiltered dataset (all statuses)
            var scoreSummary = CalculateScoreSummary(assessments);

            // Build geographic summary DTOs only when IncludeSummaries is true.
            // Uses the full unfiltered assessment set so they are consistent with the
            // top-level totals/populations regardless of whether FilterByStatus is set.
            List<GeographicLevelSummaryDto> municipalitySummaries = [];
            List<GeographicLevelSummaryDto> provinceSummaries = [];
            GeographicLevelSummaryDto? regionSummary = null;

            if (request.IncludeSummaries)
            {
                var allAreaDetails = assessments.Select(a => new ConnectivityAreaDetailDto
                {
                    LocationId = a.LocationId,
                    LocationName = string.Empty,
                    MunicipalityName = allBarangayDetails.GetValueOrDefault(a.LocationId).MunicipalityName,
                    ProvinceName = allBarangayDetails.GetValueOrDefault(a.LocationId).ProvinceName,
                    ConnectivityStatus = a.ConnectivityStatus,
                    Population = a.Population,
                    FiberCoverageScore = a.FiberCoverageScore,
                    InternetAccessScore = a.InternetAccessScore,
                    TechnologyTypeScore = a.TechnologyTypeScore,
                    QualityOfServiceScore = a.QualityOfServiceScore,
                    CoverageAvailabilityScore = a.CoverageAvailabilityScore,
                    TotalScore = a.TotalScore,
                }).ToList();

                municipalitySummaries = ConnectivityReportSummaryHelper.BuildMunicipalitySummaries(allAreaDetails);
                provinceSummaries = ConnectivityReportSummaryHelper.BuildProvinceSummaries(allAreaDetails);
                regionSummary = ConnectivityReportSummaryHelper.BuildRegionSummary(allAreaDetails);
            }

            var reportDto = new ConnectivityReportDto
            {
                TotalServedAreas = servedAreas.Count,
                ServedPopulation = servedAreas.Sum(a => a.Population ?? 0),
                TotalUnderservedAreas = underservedAreas.Count,
                UnderservedPopulation = underservedAreas.Sum(a => a.Population ?? 0),
                TotalUnservedAreas = unservedAreas.Count,
                UnservedPopulation = unservedAreas.Sum(a => a.Population ?? 0),
                Areas = areaDetails,
                ScoreSummary = scoreSummary,
                MunicipalitySummaries = municipalitySummaries,
                ProvinceSummaries = provinceSummaries,
                RegionSummary = regionSummary,
                TotalAreas = totalAreas,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                GeneratedAt = DateTime.UtcNow
            };

            return new ApiResponse<ConnectivityReportDto>
            {
                Data = reportDto,
                Message = "Connectivity report generated successfully.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate connectivity report");
            return new ApiResponse<ConnectivityReportDto>
            {
                Message = $"Failed to generate connectivity report: {ex.Message}",
                Succeeded = false
            };
        }
    }

    private ConnectivityScoreSummaryDto CalculateScoreSummary(List<Domain.Entities.ConnectivityAssessment> assessments)
    {
        if (assessments.Count == 0)
        {
            return new ConnectivityScoreSummaryDto();
        }

        return new ConnectivityScoreSummaryDto
        {
            AverageFiberCoverageScore = assessments.Average(a => a.FiberCoverageScore),
            AverageInternetAccessScore = assessments.Average(a => a.InternetAccessScore),
            AverageTechnologyTypeScore = assessments.Average(a => a.TechnologyTypeScore),
            AverageQualityOfServiceScore = assessments.Average(a => a.QualityOfServiceScore),
            AverageCoverageAvailabilityScore = assessments.Average(a => a.CoverageAvailabilityScore),
            AverageTotalScore = assessments.Average(a => a.TotalScore),
            ExcellentCount = assessments.Count(a => a.TotalScore >= 13),
            GoodCount = assessments.Count(a => a.TotalScore >= 11 && a.TotalScore <= 12),
            ModerateCount = assessments.Count(a => a.TotalScore >= 8 && a.TotalScore <= 10),
            FairCount = assessments.Count(a => a.TotalScore >= 6 && a.TotalScore <= 7),
            TotalCellTowers = assessments.Sum(a => a.CellTowerCount),
            TotalFiberNAPs = assessments.Sum(a => a.FiberNAPCount),
            TotalWifiHotspots = assessments.Sum(a => a.WifiHotspotCount),
            TotalServiceProviders = assessments.Select(a => a.DominantServiceProvider).Where(p => !string.IsNullOrEmpty(p)).Distinct().Count()
        };
    }

    private async Task<List<Domain.Entities.ConnectivityAssessment>> GetFilteredAssessmentsAsync(
        Guid? regionId, Guid? provinceId, Guid? municipalityId, LocationTypeEnum locationType, CancellationToken cancellationToken)
    {
        var query = _assessmentRepository.Query(a => a.LocationType == locationType);

        // Only include barangays with BarangayCoverage records
        var coveredBarangayIds = _barangayCoverageRepository.Query()
            .Select(bc => bc.BarangayId);

        // Apply geographic filter precedence: Municipality > Province > Region
        if (municipalityId.HasValue)
        {
            var barangayIdsInMunicipality = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.MunicipalityId == municipalityId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (barangayIdsInMunicipality.Count > 0)
            {
                query = query.Where(a => barangayIdsInMunicipality.Contains(a.LocationId));
            }
            else
            {
                query = query.Where(a => false);
            }
        }
        else if (provinceId.HasValue)
        {
            var barangayIdsInProvince = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.ProvinceId == provinceId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (barangayIdsInProvince.Count > 0)
            {
                query = query.Where(a => barangayIdsInProvince.Contains(a.LocationId));
            }
            else
            {
                query = query.Where(a => false);
            }
        }
        else if (regionId.HasValue)
        {
            var barangayIdsInRegion = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.Province.RegionId == regionId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (barangayIdsInRegion.Count > 0)
            {
                query = query.Where(a => barangayIdsInRegion.Contains(a.LocationId));
            }
            else
            {
                query = query.Where(a => false);
            }
        }
        else
        {
            // No geographic filter, just filter by covered barangays
            var allCoveredBarangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (allCoveredBarangayIds.Count > 0)
            {
                query = query.Where(a => allCoveredBarangayIds.Contains(a.LocationId));
            }
            else
            {
                query = query.Where(a => false);
            }
        }

        return await _assessmentRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<Dictionary<(Guid, LocationTypeEnum), string>> GetLocationNamesBatchAsync(
        List<(Guid LocationId, LocationTypeEnum LocationType)> locations,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<(Guid, LocationTypeEnum), string>();

        var uncachedLocations = new List<(Guid, LocationTypeEnum)>();
        foreach (var location in locations)
        {
            if (_locationNameCache.TryGetValue(location, out var cachedName))
            {
                result[location] = cachedName;
            }
            else
            {
                uncachedLocations.Add(location);
            }
        }

        if (uncachedLocations.Count == 0)
            return result;

        var locationGroups = uncachedLocations.GroupBy(l => l.Item2);

        var batchTasks = locationGroups.Select(async group =>
        {
            var locationType = group.Key;
            var locationIds = group.Select(g => g.Item1).ToList();

            try
            {
                var names = await GetLocationNamesByTypeAsync(locationType, locationIds, cancellationToken);

                foreach (var kvp in names)
                {
                    var key = (kvp.Key, locationType);
                    _locationNameCache.TryAdd(key, kvp.Value);
                    result[key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch loading location names for type '{LocationType}'", locationType);

                foreach (var locationId in locationIds)
                {
                    var fallbackName = $"Location-{locationId}";
                    var key = (locationId, locationType);
                    _locationNameCache.TryAdd(key, fallbackName);
                    result[key] = fallbackName;
                }
            }
        });

        await Task.WhenAll(batchTasks);
        return result;
    }

    private async Task<Dictionary<Guid, string>> GetLocationNamesByTypeAsync(
        LocationTypeEnum locationType,
        List<Guid> locationIds,
        CancellationToken cancellationToken = default)
    {
        if (locationIds.Count == 0)
            return new Dictionary<Guid, string>();

        return locationType switch
        {
            LocationTypeEnum.Barangay => await _barangayRepository.ToListAsync(
                _barangayRepository.QueryAsNoTracking(b => locationIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Municipality => await _municipalityRepository.ToListAsync(
                _municipalityRepository.QueryAsNoTracking(m => locationIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Provincial => await _provinceRepository.ToListAsync(
                _provinceRepository.QueryAsNoTracking(p => locationIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Regional => await _regionRepository.ToListAsync(
                _regionRepository.QueryAsNoTracking(r => locationIds.Contains(r.Id))
                    .Select(r => new { r.Id, r.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            _ => new Dictionary<Guid, string>()
        };
    }

    private async Task<Dictionary<Guid, (string MunicipalityName, string ProvinceName)>> GetBarangayDetailsAsync(
        List<Guid> barangayIds,
        CancellationToken cancellationToken = default)
    {
        if (barangayIds.Count == 0)
            return new Dictionary<Guid, (string, string)>();

        try
        {
            var barangays = await _barangayRepository.ToListAsync(
                _barangayRepository.QueryAsNoTracking(b => barangayIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        MunicipalityName = b.Municipality.Name,
                        ProvinceName = b.Municipality.Province.Name
                    }),
                cancellationToken);

            return barangays.ToDictionary(
                x => x.Id,
                x => (x.MunicipalityName, x.ProvinceName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching barangay details with municipality and province names");
            return new Dictionary<Guid, (string, string)>();
        }
    }
}
