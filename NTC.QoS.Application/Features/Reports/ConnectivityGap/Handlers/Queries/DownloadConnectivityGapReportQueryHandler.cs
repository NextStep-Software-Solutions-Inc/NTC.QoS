using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.ConnectivityGap.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.ConnectivityGap.Handlers.Queries;

public class DownloadConnectivityGapReportQueryHandler : IRequestHandler<DownloadConnectivityGapReportQuery, ApiResponse<ConnectivityGapReportDownloadResult>>
{
    private readonly IMediator _mediator;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILogger<DownloadConnectivityGapReportQueryHandler> _logger;

    public DownloadConnectivityGapReportQueryHandler(
        IMediator mediator,
        IReportGenerationService reportGenerationService,
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        ILogger<DownloadConnectivityGapReportQueryHandler> logger)
    {
        _mediator = mediator;
        _reportGenerationService = reportGenerationService;
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<ConnectivityGapReportDownloadResult>> Handle(DownloadConnectivityGapReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Downloading connectivity gap report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, FilterByStatus={FilterByStatus}, Format={Format}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, request.FilterByStatus, request.Format);

            // First, get the report data using the JSON handler
            // For downloads, we want ALL data (not paginated), so use page size 100 and fetch all pages if needed
            var reportQuery = new GetConnectivityGapReportQuery
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
                return new ApiResponse<ConnectivityGapReportDownloadResult>
                {
                    Message = reportResponse.Message ?? "Failed to generate connectivity gap report data",
                    Succeeded = false,
                    Errors = reportResponse.Errors
                };
            }

            // If there are multiple pages, fetch all remaining pages
            var allPriorityAreas = new List<PriorityAreaDto>(reportResponse.Data.PriorityAreas);
            
            if (reportResponse.Data.TotalPages > 1)
            {
                _logger.LogInformation("Fetching {PageCount} additional pages for complete report data", reportResponse.Data.TotalPages - 1);
                
                for (int page = 2; page <= reportResponse.Data.TotalPages; page++)
                {
                    var pageQuery = new GetConnectivityGapReportQuery
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
                        allPriorityAreas.AddRange(pageResponse.Data.PriorityAreas);
                    }
                }
            }

            // Create a complete report DTO with all priority areas for export
            var completeReportData = new ConnectivityGapReportDto
            {
                Summary = reportResponse.Data.Summary,
                PriorityAreas = allPriorityAreas,
                TotalPriorityAreas = reportResponse.Data.TotalPriorityAreas,
                PageNumber = 1,
                PageSize = allPriorityAreas.Count,
                TotalPages = 1,
                GeneratedAt = reportResponse.Data.GeneratedAt
            };

            // Generate location-based default title if not provided
            var defaultTitle = await GenerateLocationBasedTitleAsync(
                "Connectivity Gap Analysis Report",
                request.RegionId,
                request.ProvinceId,
                request.MunicipalityId,
                cancellationToken);

            var title = SanitizeReportTitle(request.ReportTitle, defaultTitle);
            var fileName = await GenerateFileNameAsync(
                "ConnectivityGap",
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
                fileContent = await _reportGenerationService.GenerateConnectivityGapPdfAsync(
                    completeReportData, title, cancellationToken);
                contentType = "application/pdf";
            }
            else
            {
                fileContent = await _reportGenerationService.GenerateConnectivityGapExcelAsync(
                    completeReportData, title, cancellationToken);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }

            var result = new ConnectivityGapReportDownloadResult
            {
                FileContent = fileContent,
                FileName = fileName,
                ContentType = contentType
            };

            return new ApiResponse<ConnectivityGapReportDownloadResult>
            {
                Data = result,
                Message = "Connectivity gap report downloaded successfully.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download connectivity gap report");
            return new ApiResponse<ConnectivityGapReportDownloadResult>
            {
                Message = $"Failed to download connectivity gap report: {ex.Message}",
                Succeeded = false
            };
        }
    }

    /// <summary>
    /// Generate a location-based default title for reports
    /// </summary>
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
            // Fallback to base title if there's any error fetching names
        }

        return $"{baseTitle} - National";
    }

    /// <summary>
    /// Generate a descriptive filename for downloaded reports using location names instead of UUIDs
    /// </summary>
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
            // Fallback to National if there's any error
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{reportType}_{locationName}_{timestamp}.{extension}";
    }

    /// <summary>
    /// Sanitize the report title by removing dangerous characters and ensuring reasonable length
    /// </summary>
    private static string SanitizeReportTitle(string? customTitle, string defaultTitle)
    {
        if (string.IsNullOrWhiteSpace(customTitle))
        {
            return defaultTitle;
        }

        // Remove potentially dangerous characters for file systems and PDFs
        var sanitized = customTitle.Trim();
        var invalidChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0' };

        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c.ToString(), "");
        }

        // Limit length to reasonable size (200 characters)
        if (sanitized.Length > 200)
        {
            sanitized = sanitized[..200];
        }

        // If after sanitization the title is empty, use default
        return string.IsNullOrWhiteSpace(sanitized) ? defaultTitle : sanitized;
    }
}
