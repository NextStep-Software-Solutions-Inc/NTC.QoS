using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.Connectivity.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

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

            // Fetch all filtered assessments as a lightweight projection — no DB-level pagination.
            // Municipality-level pagination is applied in-memory after grouping.
            var allAssessments = await _assessmentRepository.ToListAsync(
                filteredQuery.Select(a => new AssessmentSummaryProjection
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
                    ServiceProviderCount = a.ServiceProviderCount,
                    DominantServiceProvider = a.DominantServiceProvider,
                    AverageDownloadSpeedMbps = a.AverageDownloadSpeedMbps,
                    AverageUploadSpeedMbps = a.AverageUploadSpeedMbps,
                    SpeedTestCount = a.SpeedTestCount,
                    AssessmentDate = a.AssessmentDate
                }),
                cancellationToken);

            if (allAssessments.Count == 0)
            {
                return new ApiResponse<ConnectivityReportDto>
                {
                    Succeeded = true,
                    Message = "No assessments found matching the specified criteria.",
                    Data = new ConnectivityReportDto
                    {
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize,
                        TotalPages = 0,
                        TotalMunicipalities = 0,
                        TotalBarangays = 0,
                        GeneratedAt = DateTime.UtcNow
                    }
                };
            }

            // Compute top-level summary counts and score summary from all filtered assessments
            var scoreSummary = CalculateScoreSummary(allAssessments);
            var servedList    = allAssessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served).ToList();
            var underservedList = allAssessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved).ToList();
            var unservedList  = allAssessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved).ToList();

            // Batch-load barangay name + municipality/province info in a single DB query
            var allBarangayIds = allAssessments.Select(a => a.LocationId).ToList();
            var barangayDetails = await GetBarangayFullDetailsAsync(allBarangayIds, cancellationToken);

            // Group assessments by municipality; build ConnectivityMunicipalityDto for each group
            var allMunicipalityDtos = allAssessments
                .GroupBy(a => barangayDetails.TryGetValue(a.LocationId, out var d) ? d.MunicipalityId : Guid.Empty)
                .Where(g => g.Key != Guid.Empty)
                .Select(group =>
                {
                    barangayDetails.TryGetValue(group.First().LocationId, out var info);
                    var avgTotal = group.Average(a => a.TotalScore);
                    var overallStatus = avgTotal >= 11 ? "Served" : avgTotal >= 6 ? "Underserved" : "Unserved";

                    // Barangays sorted Unserved → Underserved → Served (worst first), then by name
                    var barangays = group
                        .OrderBy(a => a.ConnectivityStatus)
                        .ThenBy(a => barangayDetails.TryGetValue(a.LocationId, out var bd) ? bd.Name : string.Empty)
                        .Select(a =>
                        {
                            barangayDetails.TryGetValue(a.LocationId, out var bd);
                            return new ConnectivityAreaDetailDto
                            {
                                LocationId = a.LocationId,
                                LocationName = bd?.Name ?? $"Location-{a.LocationId}",
                                MunicipalityName = info?.MunicipalityName ?? string.Empty,
                                ProvinceName = info?.ProvinceName ?? string.Empty,
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
                            };
                        })
                        .ToList();

                    return new ConnectivityMunicipalityDto
                    {
                        MunicipalityId = group.Key,
                        MunicipalityName = info?.MunicipalityName ?? string.Empty,
                        ProvinceName = info?.ProvinceName ?? string.Empty,
                        TotalBarangays = group.Count(),
                        ServedBarangays = group.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served),
                        UnderservedBarangays = group.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
                        UnservedBarangays = group.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
                        TotalPopulation = group.Sum(a => a.Population ?? 0),
                        ServedPopulation = group.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served).Sum(a => a.Population ?? 0),
                        UnderservedPopulation = group.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved).Sum(a => a.Population ?? 0),
                        UnservedPopulation = group.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved).Sum(a => a.Population ?? 0),
                        AverageFiberCoverageScore = group.Average(a => a.FiberCoverageScore),
                        AverageInternetAccessScore = group.Average(a => a.InternetAccessScore),
                        AverageTechnologyTypeScore = group.Average(a => a.TechnologyTypeScore),
                        AverageQualityOfServiceScore = group.Average(a => a.QualityOfServiceScore),
                        AverageCoverageAvailabilityScore = group.Average(a => a.CoverageAvailabilityScore),
                        AverageTotalScore = avgTotal,
                        OverallConnectivityStatus = overallStatus,
                        Barangays = barangays
                    };
                })
                // Municipalities sorted Unserved → Underserved → Served (worst first), then alphabetically
                .OrderBy(m => m.OverallConnectivityStatus switch { "Unserved" => 0, "Underserved" => 1, _ => 2 })
                .ThenBy(m => m.MunicipalityName)
                .ToList();

            // Municipality-level pagination
            var totalMunicipalities = allMunicipalityDtos.Count;
            var totalPages = totalMunicipalities > 0 ? (int)Math.Ceiling(totalMunicipalities / (double)request.PageSize) : 0;

            if (totalPages > 0 && request.PageNumber > totalPages)
            {
                return new ApiResponse<ConnectivityReportDto>
                {
                    Succeeded = false,
                    Message = $"PageNumber must be between 1 and {totalPages}.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "PageNumber", [$"PageNumber must be between 1 and {totalPages}."] }
                    }
                };
            }

            var skip = (request.PageNumber - 1) * request.PageSize;
            var pagedMunicipalities = allMunicipalityDtos.Skip(skip).Take(request.PageSize).ToList();

            // Province/region summaries are optional (controlled by IncludeSummaries).
            // Municipality-level summaries are always embedded in each ConnectivityMunicipalityDto.
            List<GeographicLevelSummaryDto> provinceSummaries = [];
            GeographicLevelSummaryDto? regionSummary = null;

            if (request.IncludeSummaries)
            {
                var filteredAreaDetails = allAssessments.Select(a =>
                {
                    barangayDetails.TryGetValue(a.LocationId, out var d);
                    return new ConnectivityAreaDetailDto
                    {
                        LocationId = a.LocationId,
                        LocationName = string.Empty,
                        MunicipalityName = d?.MunicipalityName ?? string.Empty,
                        ProvinceName = d?.ProvinceName ?? string.Empty,
                        ConnectivityStatus = a.ConnectivityStatus,
                        Population = a.Population,
                        FiberCoverageScore = a.FiberCoverageScore,
                        InternetAccessScore = a.InternetAccessScore,
                        TechnologyTypeScore = a.TechnologyTypeScore,
                        QualityOfServiceScore = a.QualityOfServiceScore,
                        CoverageAvailabilityScore = a.CoverageAvailabilityScore,
                        TotalScore = a.TotalScore,
                    };
                }).ToList();

                var (munAssessedStatuses, provAssessedStatuses) = await FetchEntityAssessedStatusesAsync(
                    allBarangayIds, cancellationToken);
                var regionAssessedStatus = request.RegionId.HasValue
                    ? await FetchRegionAssessedStatusAsync(request.RegionId.Value, cancellationToken)
                    : null;

                // Enrich paged municipalities with entity-level assessed status
                foreach (var mun in pagedMunicipalities)
                {
                    if (munAssessedStatuses.TryGetValue((mun.MunicipalityName, mun.ProvinceName), out var status))
                        mun.AssessedConnectivityStatus = status;
                }

                provinceSummaries = ConnectivityReportSummaryHelper.BuildProvinceSummaries(filteredAreaDetails, provAssessedStatuses);
                regionSummary = ConnectivityReportSummaryHelper.BuildRegionSummary(filteredAreaDetails, regionAssessedStatus);
            }

            var reportDto = new ConnectivityReportDto
            {
                TotalServedAreas = servedList.Count,
                ServedPopulation = servedList.Sum(a => a.Population ?? 0),
                TotalUnderservedAreas = underservedList.Count,
                UnderservedPopulation = underservedList.Sum(a => a.Population ?? 0),
                TotalUnservedAreas = unservedList.Count,
                UnservedPopulation = unservedList.Sum(a => a.Population ?? 0),
                Municipalities = pagedMunicipalities,
                ScoreSummary = scoreSummary,
                ProvinceSummaries = provinceSummaries,
                RegionSummary = regionSummary,
                TotalMunicipalities = totalMunicipalities,
                TotalBarangays = allAssessments.Count,
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
            TotalCellTowers = summaryData.Sum(a => a.CellTowerCount),
            TotalFiberNAPs = summaryData.Sum(a => a.FiberNAPCount),
            TotalWifiHotspots = summaryData.Sum(a => a.WifiHotspotCount),
            TotalServiceProviders = summaryData.Select(a => a.DominantServiceProvider).Where(p => !string.IsNullOrEmpty(p)).Distinct().Count()
        };
    }

    /// <summary>
    /// Builds an <see cref="IQueryable{ConnectivityAssessment}"/> scoped to the specified geographic
    /// filters and restricted to barangays that have a <c>BarangayCoverage</c> record.
    /// Covered barangay IDs are materialised once into a <c>List&lt;Guid&gt;</c> so that the
    /// subsequent <c>WHERE IN (...)</c> predicate is applied consistently by EF Core across both
    /// <c>CountAsync</c> and <c>ToListAsync</c> calls (cross-repository nested IQueryable subqueries
    /// can produce inconsistent results between COUNT and SELECT paths).
    /// </summary>
    private async Task<IQueryable<Domain.Entities.ConnectivityAssessment>> BuildFilteredQueryAsync(
        Guid? regionId, Guid? provinceId, Guid? municipalityId, LocationTypeEnum locationType, CancellationToken cancellationToken)
    {
        var query = _assessmentRepository.Query(a => a.LocationType == locationType);

        // Materialise covered barangay IDs (barangays with a BarangayCoverage record).
        // Using a concrete List<Guid> ensures EF Core translates the filter identically for
        // CountAsync and ToListAsync — avoiding the inconsistency seen with nested IQueryable subqueries.
        var coveredBarangayIds = _barangayCoverageRepository.Query()
            .Select(bc => bc.BarangayId);

        List<Guid> scopedBarangayIds;
        if (municipalityId.HasValue)
        {
            scopedBarangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository
                    .Query(b => b.MunicipalityId == municipalityId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);
        }
        else if (provinceId.HasValue)
        {
            scopedBarangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository
                    .Query(b => b.Municipality.ProvinceId == provinceId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);
        }
        else if (regionId.HasValue)
        {
            scopedBarangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository
                    .Query(b => b.Municipality.Province.RegionId == regionId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);
        }
        else
        {
            // National scope: all covered barangays
            scopedBarangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository
                    .Query(b => coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);
        }

        query = scopedBarangayIds.Count > 0
            ? query.Where(a => scopedBarangayIds.Contains(a.LocationId))
            : query.Where(a => false);

        return query;
    }

    /// <summary>Lightweight projection used for summary statistics and per-municipality grouping.</summary>
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
        public int ServiceProviderCount { get; set; }
        public string? DominantServiceProvider { get; set; }
        public decimal? AverageDownloadSpeedMbps { get; set; }
        public decimal? AverageUploadSpeedMbps { get; set; }
        public int SpeedTestCount { get; set; }
        public DateTime AssessmentDate { get; set; }
    }

    /// <summary>Barangay name + municipality/province info resolved from DB in a single query.</summary>
    private sealed class BarangayFullDetail
    {
        public string Name { get; set; } = string.Empty;
        public Guid MunicipalityId { get; set; }
        public string MunicipalityName { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
    }

    private async Task<Dictionary<Guid, BarangayFullDetail>> GetBarangayFullDetailsAsync(
        List<Guid> barangayIds, CancellationToken cancellationToken = default)
    {
        if (barangayIds.Count == 0)
            return [];

        try
        {
            var rows = await _barangayRepository.ToListAsync(
                _barangayRepository.QueryAsNoTracking(b => barangayIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.MunicipalityId,
                        MunicipalityName = b.Municipality.Name,
                        ProvinceName = b.Municipality.Province.Name
                    }),
                cancellationToken);

            return rows.ToDictionary(
                x => x.Id,
                x => new BarangayFullDetail
                {
                    Name = x.Name,
                    MunicipalityId = x.MunicipalityId,
                    MunicipalityName = x.MunicipalityName,
                    ProvinceName = x.ProvinceName
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching barangay full details");
            return [];
        }
    }

    /// <summary>
    /// Fetches the pre-stored entity-level ConnectivityStatus for each unique municipality and province
    /// in the given set of barangay IDs. Returns two lookup dictionaries keyed by name for use in the
    /// summary helper.
    /// </summary>
    private async Task<(IReadOnlyDictionary<(string Name, string Province), string> MunicipalityStatuses,
                        IReadOnlyDictionary<string, string> ProvinceStatuses)>
        FetchEntityAssessedStatusesAsync(List<Guid> barangayIds, CancellationToken cancellationToken)
    {
        if (barangayIds.Count == 0)
            return (new Dictionary<(string, string), string>(), new Dictionary<string, string>());

        try
        {
            var geoInfo = await _barangayRepository.ToListAsync(
                _barangayRepository.QueryAsNoTracking(b => barangayIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.MunicipalityId,
                        MunicipalityName = b.Municipality.Name,
                        ProvinceId = b.Municipality.ProvinceId,
                        ProvinceName = b.Municipality.Province.Name
                    }),
                cancellationToken);

            var uniqueMunicipalities = geoInfo
                .GroupBy(g => g.MunicipalityId)
                .Select(g => (Id: g.Key, g.First().MunicipalityName, g.First().ProvinceName))
                .ToList();

            var munAssessments = await _assessmentRepository.GetByLocationIdsAsync(
                uniqueMunicipalities.Select(m => m.Id).ToList(),
                LocationTypeEnum.Municipality, cancellationToken);
            var munAssessmentById = munAssessments.ToDictionary(a => a.LocationId);

            var municipalityStatuses = uniqueMunicipalities
                .Where(m => munAssessmentById.ContainsKey(m.Id))
                .GroupBy(m => (m.MunicipalityName, m.ProvinceName))
                .ToDictionary(
                    g => g.Key,
                    g => munAssessmentById[g.First().Id].ConnectivityStatus.ToString());

            var uniqueProvinces = geoInfo
                .GroupBy(g => g.ProvinceId)
                .Select(g => (Id: g.Key, g.First().ProvinceName))
                .ToList();

            var provAssessments = await _assessmentRepository.GetByLocationIdsAsync(
                uniqueProvinces.Select(p => p.Id).ToList(),
                LocationTypeEnum.Provincial, cancellationToken);
            var provAssessmentById = provAssessments.ToDictionary(a => a.LocationId);

            var provinceStatuses = uniqueProvinces
                .Where(p => provAssessmentById.ContainsKey(p.Id))
                .GroupBy(p => p.ProvinceName)
                .ToDictionary(
                    g => g.Key,
                    g => provAssessmentById[g.First().Id].ConnectivityStatus.ToString());

            return (municipalityStatuses, provinceStatuses);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching entity-level assessed statuses for connectivity report");
            return (new Dictionary<(string, string), string>(), new Dictionary<string, string>());
        }
    }

    private async Task<string?> FetchRegionAssessedStatusAsync(Guid regionId, CancellationToken cancellationToken)
    {
        try
        {
            var assessment = await _assessmentRepository.GetByLocationAsync(regionId, LocationTypeEnum.Regional, cancellationToken);
            return assessment?.ConnectivityStatus.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching region entity-level assessed status for regionId {RegionId}", regionId);
            return null;
        }
    }
}
