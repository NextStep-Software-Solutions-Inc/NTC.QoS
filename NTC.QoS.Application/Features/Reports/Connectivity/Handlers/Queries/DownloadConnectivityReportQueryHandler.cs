using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.Connectivity.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.Connectivity.Handlers.Queries;

public class DownloadConnectivityReportQueryHandler : IRequestHandler<DownloadConnectivityReportQuery, ApiResponse<ConnectivityReportDownloadResult>>
{
    private const int DownloadPageSize = 100;

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

            // Fetch the full unfiltered dataset once (all pages).  If FilterByStatus is set we
            // derive the filtered Areas list in-memory rather than issuing a second full DB scan.
            var unfilteredQuery = new GetConnectivityReportQuery
            {
                RegionId = request.RegionId,
                ProvinceId = request.ProvinceId,
                MunicipalityId = request.MunicipalityId,
                FilterByStatus = null,
                PageNumber = 1,
                PageSize = DownloadPageSize
            };

            var reportResponse = await _mediator.Send(unfilteredQuery, cancellationToken);

            if (!reportResponse.Succeeded || reportResponse.Data == null)
            {
                return new ApiResponse<ConnectivityReportDownloadResult>
                {
                    Message = reportResponse.Message ?? "Failed to generate connectivity report data",
                    Succeeded = false,
                    Errors = reportResponse.Errors
                };
            }

            // Collect all pages of the unfiltered dataset
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
                        FilterByStatus = null,
                        PageNumber = page,
                        PageSize = DownloadPageSize
                    };

                    var pageResponse = await _mediator.Send(pageQuery, cancellationToken);

                    if (pageResponse.Succeeded && pageResponse.Data != null)
                    {
                        allAreas.AddRange(pageResponse.Data.Areas);
                    }
                }
            }

            // Derive the export Areas list in-memory; geographic summaries always use allAreas
            var filteredAreas = request.FilterByStatus.HasValue
                ? allAreas.Where(a => a.ConnectivityStatus == request.FilterByStatus.Value).ToList()
                : allAreas;

            // Create a complete report DTO: summaries from the full dataset, Areas from the filtered list
            var completeReportData = new ConnectivityReportDto
            {
                TotalServedAreas = reportResponse.Data.TotalServedAreas,
                ServedPopulation = reportResponse.Data.ServedPopulation,
                TotalUnderservedAreas = reportResponse.Data.TotalUnderservedAreas,
                UnderservedPopulation = reportResponse.Data.UnderservedPopulation,
                TotalUnservedAreas = reportResponse.Data.TotalUnservedAreas,
                UnservedPopulation = reportResponse.Data.UnservedPopulation,
                Areas = filteredAreas,
                ScoreSummary = reportResponse.Data.ScoreSummary,
                MunicipalitySummaries = ConnectivityReportSummaryHelper.BuildMunicipalitySummaries(allAreas),
                ProvinceSummaries = ConnectivityReportSummaryHelper.BuildProvinceSummaries(allAreas),
                RegionSummary = ConnectivityReportSummaryHelper.BuildRegionSummary(allAreas),
                TotalAreas = request.FilterByStatus.HasValue ? filteredAreas.Count : reportResponse.Data.TotalAreas,
                PageNumber = 1,
                PageSize = filteredAreas.Count,
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
    // Shared logic lives in ConnectivityReportSummaryHelper (NTC.QoS.Domain.Helper).
}
