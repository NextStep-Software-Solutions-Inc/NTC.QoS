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

            // Validate FilterByStatus: reject undefined enum values to avoid silently empty results
            if (request.FilterByStatus.HasValue && !Enum.IsDefined(typeof(ConnectivityStatusEnum), request.FilterByStatus.Value))
            {
                var validValues = string.Join(", ", Enum.GetValues<ConnectivityStatusEnum>().Select(v => $"{v} ({(int)v})"));
                var invalidStatusMsg = $"Invalid FilterByStatus value '{(int)request.FilterByStatus.Value}'. Valid values are: {validValues}.";
                return new ApiResponse<ConnectivityReportDto>
                {
                    Succeeded = false,
                    Message = invalidStatusMsg,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "FilterByStatus", [invalidStatusMsg] }
                    }
                };
            }

            // Build the base IQueryable (geographic + coverage filters applied, not yet materialized)
            var baseQuery = await BuildFilteredQueryAsync(
                request.RegionId, request.ProvinceId, request.MunicipalityId, LocationTypeEnum.Barangay, cancellationToken);

            // Apply optional status filter; by default include all statuses (unified view)
            var filteredQuery = request.FilterByStatus.HasValue
                ? baseQuery.Where(a => a.ConnectivityStatus == request.FilterByStatus.Value)
                : baseQuery;

            // Sort at the DB level: unserved first, then by population and score
            var sortedQuery = filteredQuery
                .OrderBy(a => a.ConnectivityStatus)
                .ThenByDescending(a => a.Population ?? 0)
                .ThenByDescending(a => a.TotalScore);

            // Count and paginate at the DB level — no full dataset materialization per page
            var totalAreas = await _assessmentRepository.CountAsync(sortedQuery, cancellationToken);
            var totalPages = totalAreas > 0 ? (int)Math.Ceiling(totalAreas / (double)request.PageSize) : 0;
            var skip = (request.PageNumber - 1) * request.PageSize;
            var pageAssessments = await _assessmentRepository.ToListAsync(
                sortedQuery.Skip(skip).Take(request.PageSize), cancellationToken);

            // Fetch a small projection from the full base query for summary statistics.
            // This avoids materializing the complete entity graph just to compute aggregates.
            var summaryData = await _assessmentRepository.ToListAsync(
                baseQuery.Select(a => new AssessmentSummaryProjection
                {
                    LocationId = a.LocationId,
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
                    DominantServiceProvider = a.DominantServiceProvider
                }),
                cancellationToken);

            var servedSummary = summaryData.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served).ToList();
            var underservedSummary = summaryData.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved).ToList();
            var unservedSummary = summaryData.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved).ToList();

            var scoreSummary = CalculateScoreSummary(summaryData);

            // Load location names (barangay name) for the page only
            var locationNames = await GetLocationNamesBatchAsync(
                pageAssessments.Select(a => (a.LocationId, a.LocationType)).ToList(),
                cancellationToken);

            // Barangay details (municipality/province names):
            //   - IncludeSummaries=true:  load for ALL assessments so geographic summaries are accurate
            //   - IncludeSummaries=false: load only for the current page to keep the query small
            var detailIds = request.IncludeSummaries
                ? summaryData.Select(a => a.LocationId).ToList()
                : pageAssessments.Select(a => a.LocationId).ToList();
            var allBarangayDetails = await GetBarangayDetailsAsync(detailIds, cancellationToken);

            var areaDetails = pageAssessments.Select(a => new ConnectivityAreaDetailDto
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

            // Build geographic summary DTOs only when IncludeSummaries is true.
            // Uses the full unfiltered assessment set so they are consistent with the
            // top-level totals/populations regardless of whether FilterByStatus is set.
            // Empty list defaults mirror the ConnectivityReportDto property defaults;
            // regionSummary starts null because the DTO property is GeographicLevelSummaryDto?
            List<GeographicLevelSummaryDto> municipalitySummaries = [];
            List<GeographicLevelSummaryDto> provinceSummaries = [];
            GeographicLevelSummaryDto? regionSummary = null;

            if (request.IncludeSummaries)
            {
                var allAreaDetails = summaryData.Select(a => new ConnectivityAreaDetailDto
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
                TotalServedAreas = servedSummary.Count,
                ServedPopulation = servedSummary.Sum(a => a.Population ?? 0),
                TotalUnderservedAreas = underservedSummary.Count,
                UnderservedPopulation = underservedSummary.Sum(a => a.Population ?? 0),
                TotalUnservedAreas = unservedSummary.Count,
                UnservedPopulation = unservedSummary.Sum(a => a.Population ?? 0),
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

    private ConnectivityScoreSummaryDto CalculateScoreSummary(IReadOnlyList<AssessmentSummaryProjection> summaryData)
    {
        if (summaryData.Count == 0)
        {
            return new ConnectivityScoreSummaryDto();
        }

        return new ConnectivityScoreSummaryDto
        {
            AverageFiberCoverageScore = summaryData.Average(a => a.FiberCoverageScore),
            AverageInternetAccessScore = summaryData.Average(a => a.InternetAccessScore),
            AverageTechnologyTypeScore = summaryData.Average(a => a.TechnologyTypeScore),
            AverageQualityOfServiceScore = summaryData.Average(a => a.QualityOfServiceScore),
            AverageCoverageAvailabilityScore = summaryData.Average(a => a.CoverageAvailabilityScore),
            AverageTotalScore = summaryData.Average(a => a.TotalScore),
            ExcellentCount = summaryData.Count(a => a.TotalScore >= 13),
            GoodCount = summaryData.Count(a => a.TotalScore >= 11 && a.TotalScore <= 12),
            ModerateCount = summaryData.Count(a => a.TotalScore >= 8 && a.TotalScore <= 10),
            FairCount = summaryData.Count(a => a.TotalScore >= 6 && a.TotalScore <= 7),
            TotalCellTowers = summaryData.Sum(a => a.CellTowerCount),
            TotalFiberNAPs = summaryData.Sum(a => a.FiberNAPCount),
            TotalWifiHotspots = summaryData.Sum(a => a.WifiHotspotCount),
            TotalServiceProviders = summaryData.Select(a => a.DominantServiceProvider).Where(p => !string.IsNullOrEmpty(p)).Distinct().Count()
        };
    }

    /// <summary>
    /// Builds an <see cref="IQueryable{ConnectivityAssessment}"/> scoped to the specified geographic
    /// filters and restricted to barangays that have a <c>BarangayCoverage</c> record.
    /// The query is <b>not</b> materialised; callers apply further predicates, sorting, counting,
    /// and pagination before executing.
    /// </summary>
    private async Task<IQueryable<Domain.Entities.ConnectivityAssessment>> BuildFilteredQueryAsync(
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

            query = barangayIdsInMunicipality.Count > 0
                ? query.Where(a => barangayIdsInMunicipality.Contains(a.LocationId))
                : query.Where(a => false);
        }
        else if (provinceId.HasValue)
        {
            var barangayIdsInProvince = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.ProvinceId == provinceId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            query = barangayIdsInProvince.Count > 0
                ? query.Where(a => barangayIdsInProvince.Contains(a.LocationId))
                : query.Where(a => false);
        }
        else if (regionId.HasValue)
        {
            var barangayIdsInRegion = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.Province.RegionId == regionId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            query = barangayIdsInRegion.Count > 0
                ? query.Where(a => barangayIdsInRegion.Contains(a.LocationId))
                : query.Where(a => false);
        }
        else
        {
            // No geographic filter, just filter by covered barangays
            var allCoveredBarangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            query = allCoveredBarangayIds.Count > 0
                ? query.Where(a => allCoveredBarangayIds.Contains(a.LocationId))
                : query.Where(a => false);
        }

        return query;
    }

    /// <summary>Lightweight projection used for summary statistics and geographic summaries.</summary>
    private sealed class AssessmentSummaryProjection
    {
        public Guid LocationId { get; set; }
        public ConnectivityStatusEnum ConnectivityStatus { get; set; }
        public int? Population { get; set; }
        public int FiberCoverageScore { get; set; }
        public int InternetAccessScore { get; set; }
        public int TechnologyTypeScore { get; set; }
        public int QualityOfServiceScore { get; set; }
        public int CoverageAvailabilityScore { get; set; }
        public int TotalScore { get; set; }
        public int CellTowerCount { get; set; }
        public int FiberNAPCount { get; set; }
        public int WifiHotspotCount { get; set; }
        public string? DominantServiceProvider { get; set; }
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
