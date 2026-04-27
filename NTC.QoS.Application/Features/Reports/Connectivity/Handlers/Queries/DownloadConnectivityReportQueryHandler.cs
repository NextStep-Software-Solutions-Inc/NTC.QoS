using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.Connectivity.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.Connectivity.Handlers.Queries;

public class DownloadConnectivityReportQueryHandler : IRequestHandler<DownloadConnectivityReportQuery, ApiResponse<ConnectivityReportDownloadResult>>
{
    private readonly IMediator _mediator;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILogger<DownloadConnectivityReportQueryHandler> _logger;

    public DownloadConnectivityReportQueryHandler(
        IMediator mediator,
        IReportGenerationService reportGenerationService,
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        ILogger<DownloadConnectivityReportQueryHandler> logger)
    {
        _mediator = mediator;
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

            // First, get the report data using the JSON handler
            // For downloads, we want ALL data (not paginated), so use page size 100 and fetch all pages if needed
            var reportQuery = new GetConnectivityReportQuery
            {
                RegionId = request.RegionId,
                ProvinceId = request.ProvinceId,
                MunicipalityId = request.MunicipalityId,
                FilterByStatus = request.FilterByStatus,
                PageNumber = 1,
                PageSize = 100  // Max page size
            };

            var reportResponse = await _mediator.Send(reportQuery, cancellationToken);

            if (!reportResponse.Succeeded || reportResponse.Data == null)
            {
                return new ApiResponse<ConnectivityReportDownloadResult>
                {
                    Message = reportResponse.Message ?? "Failed to generate connectivity report data",
                    Succeeded = false,
                    Errors = reportResponse.Errors
                };
            }

            // If there are multiple pages, fetch all remaining pages
            var allAreas = new List<ConnectivityAreaDetailDto>(reportResponse.Data.Areas);
            
            if (reportResponse.Data.TotalPages > 1)
            {
                _logger.LogInformation("Fetching {PageCount} additional pages for complete report data", reportResponse.Data.TotalPages - 1);
                
                for (int page = 2; page <= reportResponse.Data.TotalPages; page++)
                {
                    var pageQuery = new GetConnectivityReportQuery
                    {
                        RegionId = request.RegionId,
                        ProvinceId = request.ProvinceId,
                        MunicipalityId = request.MunicipalityId,
                        FilterByStatus = request.FilterByStatus,
                        PageNumber = page,
                        PageSize = 100
                    };

                    var pageResponse = await _mediator.Send(pageQuery, cancellationToken);
                    
                    if (pageResponse.Succeeded && pageResponse.Data != null)
                    {
                        allAreas.AddRange(pageResponse.Data.Areas);
                    }
                }
            }

            // Create a complete report DTO with all areas for export
            var completeReportData = new ConnectivityReportDto
            {
                TotalServedAreas = reportResponse.Data.TotalServedAreas,
                ServedPopulation = reportResponse.Data.ServedPopulation,
                TotalUnderservedAreas = reportResponse.Data.TotalUnderservedAreas,
                UnderservedPopulation = reportResponse.Data.UnderservedPopulation,
                TotalUnservedAreas = reportResponse.Data.TotalUnservedAreas,
                UnservedPopulation = reportResponse.Data.UnservedPopulation,
                Areas = allAreas,
                ScoreSummary = reportResponse.Data.ScoreSummary,
                MunicipalitySummaries = BuildMunicipalitySummaries(allAreas),
                ProvinceSummaries = BuildProvinceSummaries(allAreas),
                RegionSummary = BuildRegionSummary(allAreas),
                TotalAreas = reportResponse.Data.TotalAreas,
                PageNumber = 1,
                PageSize = allAreas.Count,
                TotalPages = 1,
                GeneratedAt = reportResponse.Data.GeneratedAt
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
                    completeReportData, title, cancellationToken);
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

    // ── Geographic summary builders ──────────────────────────────────────────

    private static List<GeographicLevelSummaryDto> BuildMunicipalitySummaries(
        List<ConnectivityAreaDetailDto> areas)
    {
        return areas
            .GroupBy(a => new
            {
                MunicipalityName = a.MunicipalityName ?? "Unknown",
                ProvinceName = a.ProvinceName ?? "Unknown"
            })
            .Select(g => AggregateToSummary(
                name: g.Key.MunicipalityName,
                parentName: g.Key.ProvinceName,
                level: "Municipality",
                areas: g.ToList()))
            .OrderBy(s => s.ParentName)
            .ThenBy(s => s.Name)
            .ToList();
    }

    private static List<GeographicLevelSummaryDto> BuildProvinceSummaries(
        List<ConnectivityAreaDetailDto> areas)
    {
        return areas
            .GroupBy(a => a.ProvinceName ?? "Unknown")
            .Select(g => AggregateToSummary(
                name: g.Key,
                parentName: null,
                level: "Province",
                areas: g.ToList()))
            .OrderBy(s => s.Name)
            .ToList();
    }

    private static GeographicLevelSummaryDto? BuildRegionSummary(
        List<ConnectivityAreaDetailDto> areas)
    {
        if (areas.Count == 0) return null;

        return AggregateToSummary(
            name: "All Areas",
            parentName: null,
            level: "Region",
            areas: areas);
    }

    private static GeographicLevelSummaryDto AggregateToSummary(
        string name,
        string? parentName,
        string level,
        List<ConnectivityAreaDetailDto> areas)
    {
        if (areas.Count == 0)
        {
            return new GeographicLevelSummaryDto
            {
                Name = name,
                ParentName = parentName,
                Level = level,
                OverallConnectivityStatus = "Unserved"
            };
        }

        var avgTotal = areas.Average(a => a.TotalScore);

        return new GeographicLevelSummaryDto
        {
            Name = name,
            ParentName = parentName,
            Level = level,
            TotalBarangays = areas.Count,
            ServedBarangays = areas.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served),
            UnderservedBarangays = areas.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
            UnservedBarangays = areas.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
            TotalPopulation = areas.Sum(a => (long)(a.Population ?? 0)),
            ServedPopulation = areas
                .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served)
                .Sum(a => (long)(a.Population ?? 0)),
            UnderservedPopulation = areas
                .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved)
                .Sum(a => (long)(a.Population ?? 0)),
            UnservedPopulation = areas
                .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved)
                .Sum(a => (long)(a.Population ?? 0)),
            AverageFiberCoverageScore = areas.Average(a => a.FiberCoverageScore),
            AverageInternetAccessScore = areas.Average(a => a.InternetAccessScore),
            AverageTechnologyTypeScore = areas.Average(a => a.TechnologyTypeScore),
            AverageQualityOfServiceScore = areas.Average(a => a.QualityOfServiceScore),
            AverageCoverageAvailabilityScore = areas.Average(a => a.CoverageAvailabilityScore),
            AverageTotalScore = avgTotal,
            ExcellentCount = areas.Count(a => a.TotalScore >= 13),
            GoodCount = areas.Count(a => a.TotalScore >= 11 && a.TotalScore <= 12),
            ModerateCount = areas.Count(a => a.TotalScore >= 8 && a.TotalScore <= 10),
            FairCount = areas.Count(a => a.TotalScore >= 6 && a.TotalScore <= 7),
            OverallConnectivityStatus = avgTotal >= 11 ? "Served"
                : avgTotal >= 6 ? "Underserved"
                : "Unserved"
        };
    }
}
