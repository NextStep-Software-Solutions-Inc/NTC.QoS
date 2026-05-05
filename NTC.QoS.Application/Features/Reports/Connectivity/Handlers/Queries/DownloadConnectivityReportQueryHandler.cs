using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.Connectivity.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.Connectivity.Handlers.Queries;

public class DownloadConnectivityReportQueryHandler : MediatR.IRequestHandler<DownloadConnectivityReportQuery, ApiResponse<ConnectivityReportDownloadResult>>
{
    private readonly IConnectivityAssessmentRepository _assessmentRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILogger<DownloadConnectivityReportQueryHandler> _logger;

    public DownloadConnectivityReportQueryHandler(
        IConnectivityAssessmentRepository assessmentRepository,
        IBarangayRepository barangayRepository,
        IBarangayCoverageRepository barangayCoverageRepository,
        IReportGenerationService reportGenerationService,
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        ILogger<DownloadConnectivityReportQueryHandler> logger)
    {
        _assessmentRepository = assessmentRepository;
        _barangayRepository = barangayRepository;
        _barangayCoverageRepository = barangayCoverageRepository;
        _reportGenerationService = reportGenerationService;
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<ConnectivityReportDownloadResult>> Handle(DownloadConnectivityReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Downloading unified connectivity report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, FilterByStatus={FilterByStatus}, Format={Format}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, request.FilterByStatus, request.Format);

            // Validate FilterByStatus before fetching any data
            if (request.FilterByStatus.HasValue && !Enum.IsDefined(typeof(ConnectivityStatusEnum), request.FilterByStatus.Value))
            {
                var validValues = string.Join(", ", Enum.GetValues<ConnectivityStatusEnum>().Select(v => $"{v} ({(int)v})"));
                var invalidStatusMsg = $"Invalid FilterByStatus value '{(int)request.FilterByStatus.Value}'. Valid values are: {validValues}.";
                return new ApiResponse<ConnectivityReportDownloadResult>
                {
                    Succeeded = false,
                    Message = invalidStatusMsg,
                    Errors = new Dictionary<string, string[]> { { "FilterByStatus", [invalidStatusMsg] } }
                };
            }

            // Fetch the full unfiltered dataset in a single DB call (no pagination loop).
            // Geographic summaries and summary totals are computed from this complete set.
            var baseQuery = await BuildBaseQueryAsync(
                request.RegionId, request.ProvinceId, request.MunicipalityId, cancellationToken);

            var allAssessments = await _assessmentRepository.ToListAsync(baseQuery, cancellationToken);

            _logger.LogInformation("Fetched {Count} assessments for connectivity report download", allAssessments.Count);

            // Batch-load location names (barangay names) and municipality/province details
            var allLocationIds = allAssessments.Select(a => a.LocationId).ToList();

            var barangayNames = await GetBarangayNamesAsync(allLocationIds, cancellationToken);
            var barangayDetails = await GetBarangayDetailsAsync(allLocationIds, cancellationToken);

            // Project all assessments to ConnectivityAreaDetailDto
            var allAreas = allAssessments.Select(a => new ConnectivityAreaDetailDto
            {
                LocationId = a.LocationId,
                LocationName = barangayNames.GetValueOrDefault(a.LocationId, $"Location-{a.LocationId}"),
                MunicipalityName = barangayDetails.GetValueOrDefault(a.LocationId).MunicipalityName,
                ProvinceName = barangayDetails.GetValueOrDefault(a.LocationId).ProvinceName,
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

            // Derive the export Areas list in-memory; geographic summaries follow the filtered areas
            var filteredAreas = request.FilterByStatus.HasValue
                ? allAreas.Where(a => a.ConnectivityStatus == request.FilterByStatus.Value).ToList()
                : allAreas;

            var filteredAssessments = request.FilterByStatus.HasValue
                ? allAssessments.Where(a => a.ConnectivityStatus == request.FilterByStatus.Value).ToList()
                : allAssessments;

            // Compute top-level status totals from the filtered set
            var servedAreas = filteredAssessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served).ToList();
            var underservedAreas = filteredAssessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved).ToList();
            var unservedAreas = filteredAssessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved).ToList();

            var scoreSummary = CalculateScoreSummary(filteredAssessments);

            // Fetch entity-level assessed statuses for geographic summaries
            var (munAssessedStatuses, provAssessedStatuses) = await FetchEntityAssessedStatusesAsync(
                allLocationIds, cancellationToken);
            var regionAssessedStatus = request.RegionId.HasValue
                ? await FetchRegionAssessedStatusAsync(request.RegionId.Value, cancellationToken)
                : null;

            // Create a complete report DTO: top-level totals, Areas and
            // geographic summaries from the filtered list
            var completeReportData = new ConnectivityReportDto
            {
                TotalServedAreas = servedAreas.Count,
                ServedPopulation = servedAreas.Sum(a => a.Population ?? 0),
                TotalUnderservedAreas = underservedAreas.Count,
                UnderservedPopulation = underservedAreas.Sum(a => a.Population ?? 0),
                TotalUnservedAreas = unservedAreas.Count,
                UnservedPopulation = unservedAreas.Sum(a => a.Population ?? 0),
                Areas = filteredAreas,
                ScoreSummary = scoreSummary,
                MunicipalitySummaries = ConnectivityReportSummaryHelper.BuildMunicipalitySummaries(filteredAreas, munAssessedStatuses),
                ProvinceSummaries = ConnectivityReportSummaryHelper.BuildProvinceSummaries(filteredAreas, provAssessedStatuses),
                RegionSummary = ConnectivityReportSummaryHelper.BuildRegionSummary(filteredAreas, regionAssessedStatus),
                TotalAreas = request.FilterByStatus.HasValue ? filteredAreas.Count : allAreas.Count,
                PageNumber = 1,
                PageSize = filteredAreas.Count,
                TotalPages = 1,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate location-based default title if not provided
            var defaultTitle = await GenerateLocationBasedTitleAsync(
                "Connectivity Report",
                request.RegionId,
                request.ProvinceId,
                request.MunicipalityId,
                cancellationToken);

            var title = SanitizeReportTitle(request.ReportTitle, defaultTitle);
            var fileName = await GenerateFileNameAsync(
                "ConnectivityReport",
                request.Format == ReportFormat.Pdf ? "pdf" : "xlsx",
                request.RegionId,
                request.ProvinceId,
                request.MunicipalityId,
                cancellationToken);

            // Generate the file based on format
            byte[] fileContent;
            string contentType;

            if (request.Format == ReportFormat.Pdf)
            {
                fileContent = await _reportGenerationService.GenerateConnectivityPdfAsync(
                    completeReportData, title, request.FilterByStatus, cancellationToken);
                contentType = "application/pdf";
            }
            else
            {
                fileContent = await _reportGenerationService.GenerateConnectivityExcelAsync(
                    completeReportData, title, cancellationToken);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }

            var result = new ConnectivityReportDownloadResult
            {
                FileContent = fileContent,
                FileName = fileName,
                ContentType = contentType
            };

            return new ApiResponse<ConnectivityReportDownloadResult>
            {
                Data = result,
                Message = "Connectivity report downloaded successfully.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download connectivity report");
            return new ApiResponse<ConnectivityReportDownloadResult>
            {
                Message = $"Failed to download connectivity report: {ex.Message}",
                Succeeded = false
            };
        }
    }

    /// <summary>
    /// Builds an <see cref="IQueryable{ConnectivityAssessment}"/> filtered to barangays that have a
    /// <c>BarangayCoverage</c> record and are within the specified geographic scope.
    /// The query is <b>not</b> materialized; callers execute it as a single DB read.
    /// All filtering is expressed as IQueryable subqueries so the database resolves them in a
    /// single round-trip without pulling large ID lists into application memory.
    /// </summary>
    private async Task<IQueryable<Domain.Entities.ConnectivityAssessment>> BuildBaseQueryAsync(
        Guid? regionId, Guid? provinceId, Guid? municipalityId, CancellationToken cancellationToken)
    {
        var query = _assessmentRepository.Query(a => a.LocationType == LocationTypeEnum.Barangay);

        // Materialise covered barangay IDs (barangays with a BarangayCoverage record).
        // Using a concrete List<Guid> ensures EF Core translates the filter identically for
        // both CountAsync and ToListAsync — avoiding inconsistencies from nested cross-repository
        // IQueryable subqueries.
        var coveredBarangayIds = _barangayCoverageRepository.Query().Select(bc => bc.BarangayId);

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

    private async Task<Dictionary<Guid, string>> GetBarangayNamesAsync(
        List<Guid> barangayIds, CancellationToken cancellationToken)
    {
        if (barangayIds.Count == 0)
            return [];

        try
        {
            var items = await _barangayRepository.ToListAsync(
                _barangayRepository.QueryAsNoTracking(b => barangayIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Name }),
                cancellationToken);
            return items.ToDictionary(x => x.Id, x => x.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching barangay names for download report");
            return [];
        }
    }

    private async Task<Dictionary<Guid, (string MunicipalityName, string ProvinceName)>> GetBarangayDetailsAsync(
        List<Guid> barangayIds, CancellationToken cancellationToken)
    {
        if (barangayIds.Count == 0)
            return [];

        try
        {
            var items = await _barangayRepository.ToListAsync(
                _barangayRepository.QueryAsNoTracking(b => barangayIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        MunicipalityName = b.Municipality.Name,
                        ProvinceName = b.Municipality.Province.Name
                    }),
                cancellationToken);
            return items.ToDictionary(x => x.Id, x => (x.MunicipalityName, x.ProvinceName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching barangay municipality/province details for download report");
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
            var munById = munAssessments.ToDictionary(a => a.LocationId);

            var municipalityStatuses = uniqueMunicipalities
                .Where(m => munById.ContainsKey(m.Id))
                .GroupBy(m => (m.MunicipalityName, m.ProvinceName))
                .ToDictionary(
                    g => g.Key,
                    g => munById[g.First().Id].ConnectivityStatus.ToString());

            var uniqueProvinces = geoInfo
                .GroupBy(g => g.ProvinceId)
                .Select(g => (Id: g.Key, g.First().ProvinceName))
                .ToList();

            var provAssessments = await _assessmentRepository.GetByLocationIdsAsync(
                uniqueProvinces.Select(p => p.Id).ToList(),
                LocationTypeEnum.Provincial, cancellationToken);
            var provById = provAssessments.ToDictionary(a => a.LocationId);

            var provinceStatuses = uniqueProvinces
                .Where(p => provById.ContainsKey(p.Id))
                .GroupBy(p => p.ProvinceName)
                .ToDictionary(
                    g => g.Key,
                    g => provById[g.First().Id].ConnectivityStatus.ToString());

            return (municipalityStatuses, provinceStatuses);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching entity-level assessed statuses for connectivity download report");
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

    private static ConnectivityScoreSummaryDto CalculateScoreSummary(
        IReadOnlyList<Domain.Entities.ConnectivityAssessment> assessments)
    {
        if (assessments.Count == 0)
            return new ConnectivityScoreSummaryDto();

        return new ConnectivityScoreSummaryDto
        {
            AverageFiberCoverageScore = assessments.Average(a => a.FiberCoverageScore),
            AverageInternetAccessScore = assessments.Average(a => a.InternetAccessScore),
            AverageTechnologyTypeScore = assessments.Average(a => a.TechnologyTypeScore),
            AverageQualityOfServiceScore = assessments.Average(a => a.QualityOfServiceScore),
            AverageCoverageAvailabilityScore = assessments.Average(a => a.CoverageAvailabilityScore),
            AverageTotalScore = assessments.Average(a => a.TotalScore),
            TotalCellTowers = assessments.Sum(a => a.CellTowerCount),
            TotalFiberNAPs = assessments.Sum(a => a.FiberNAPCount),
            TotalWifiHotspots = assessments.Sum(a => a.WifiHotspotCount),
            TotalServiceProviders = assessments
                .Select(a => a.DominantServiceProvider)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .Count()
        };
    }

    private async Task<string> GenerateLocationBasedTitleAsync(
        string baseTitle,
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (municipalityId.HasValue)
            {
                var municipalityQuery = _municipalityRepository.Query(m => m.Id == municipalityId.Value);
                var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

                if (municipality != null)
                {
                    return $"{baseTitle} - {municipality.Name}";
                }
            }
            else if (provinceId.HasValue)
            {
                var provinceQuery = _provinceRepository.Query(p => p.Id == provinceId.Value);
                var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

                if (province != null)
                {
                    return $"{baseTitle} - {province.Name}";
                }
            }
            else if (regionId.HasValue)
            {
                var regionQuery = _regionRepository.Query(r => r.Id == regionId.Value);
                var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

                if (region != null)
                {
                    return $"{baseTitle} - {region.Name}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching location names for report title");
        }

        return $"{baseTitle} - National";
    }

    private async Task<string> GenerateFileNameAsync(
        string reportType,
        string extension,
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        CancellationToken cancellationToken = default)
    {
        var locationName = "National";

        try
        {
            if (municipalityId.HasValue)
            {
                var municipalityQuery = _municipalityRepository.Query(m => m.Id == municipalityId.Value);
                var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

                if (municipality != null)
                {
                    locationName = municipality.Name.Replace(" ", "_");
                }
            }
            else if (provinceId.HasValue)
            {
                var provinceQuery = _provinceRepository.Query(p => p.Id == provinceId.Value);
                var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

                if (province != null)
                {
                    locationName = province.Name.Replace(" ", "_");
                }
            }
            else if (regionId.HasValue)
            {
                var regionQuery = _regionRepository.Query(r => r.Id == regionId.Value);
                var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

                if (region != null)
                {
                    locationName = region.Name.Replace(" ", "_");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching location names for filename");
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{reportType}_{locationName}_{timestamp}.{extension}";
    }

    private static string SanitizeReportTitle(string? customTitle, string defaultTitle)
    {
        if (string.IsNullOrWhiteSpace(customTitle))
        {
            return defaultTitle;
        }

        var sanitized = customTitle.Trim();
        var invalidChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0' };

        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c.ToString(), "");
        }

        if (sanitized.Length > 200)
        {
            sanitized = sanitized[..200];
        }

        return string.IsNullOrWhiteSpace(sanitized) ? defaultTitle : sanitized;
    }
}
