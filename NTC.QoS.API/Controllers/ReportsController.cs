using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Application.Validators.Reports;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;
using Hangfire;
using System.Security.Claims;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("reports", Name = "reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConnectivityReportingService _connectivityReportingService;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMemoryCache _memoryCache;
    private readonly IBlobStorageService _blobStorageService;

    public ReportsController(
        IMediator mediator, 
        IConnectivityReportingService connectivityReportingService,
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        IBackgroundJobClient backgroundJobClient,
        IMemoryCache memoryCache,
        IBlobStorageService blobStorageService)
    {
        _mediator = mediator;
        _connectivityReportingService = connectivityReportingService;
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _backgroundJobClient = backgroundJobClient;
        _memoryCache = memoryCache;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Generate a comprehensive wireless broadband executive summary report for municipalities and barangays.
    /// Shows wireless provider coverage with technology generations (2G, 3G, 4G, 5G) and connectivity scores.
    /// Supports pagination to handle large datasets efficiently.
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="filterByStatus">Optional filter by service status (Served=2, Underserved=1, Unserved=0)</param>
    /// <param name="forceGenerateConnectivityAssessment">If true, forces generation of new connectivity assessments even if recent ones exist</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated wireless broadband executive summary with municipality/barangay breakdown</returns>
    [HttpGet("wireless-broadband")]
    [RequirePermission(Permissions.ViewReports)]
    public async Task<ActionResult<ApiResponse<WirelessBroadbandExecutiveSummaryDto>>> GetWirelessBroadbandReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ServiceStatusEnum? filterByStatus = null,
        [FromQuery] bool forceGenerateConnectivityAssessment = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var request = new GetWirelessBroadbandExecutiveSummaryQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            FilterByStatus = filterByStatus,
            ForceGenerateConnectivityAssessment = forceGenerateConnectivityAssessment,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var report = await _mediator.Send(request);
        return report.Succeeded ? Ok(report) : BadRequest(report);
    }

    /// <summary>
    /// Generate and download wireless broadband coverage report in PDF or Excel format.
    /// When includeScores is true, includes connectivity scores and service status.
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="filterByStatus">Optional filter by service status (Served=2, Underserved=1, Unserved=0). Only applicable when includeScores is true.</param>
    /// <param name="format">Report format: Excel (0) or Pdf (1). Default: Excel</param>
    /// <param name="includeScores">When true (default), includes connectivity scores and service status in the report.</param>
    /// <returns>PDF or Excel file download</returns>
    [HttpGet("wireless-broadband/download")]
    [RequirePermission(Permissions.ExportReports)]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<WirelessBroadbandReportResult>), 400)]
    public async Task<IActionResult> DownloadWirelessBroadbandReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ServiceStatusEnum? filterByStatus = null,
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] bool includeScores = true)
    {
        // Validate that FilterByStatus is only used when IncludeScores is true
        if (!includeScores && filterByStatus.HasValue)
        {
            return BadRequest(new ApiResponse<WirelessBroadbandReportResult>
            {
                Succeeded = false,
                Message = "FilterByStatus parameter can only be used when includeScores is true. " +
                         "Service status filtering requires connectivity scores to be calculated."
            });
        }

        var request = new GetWirelessBroadbandReportQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            FilterByStatus = filterByStatus,
            Format = format,
            IncludeScores = includeScores
        };

        var result = await _mediator.Send(request);

        if (!result.Succeeded || result.Data == null)
            return BadRequest(result);

        var contentType = format == ReportFormat.Pdf
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(
            result.Data.FileContent,
            contentType,
            result.Data.FileName);
    }

    /// <summary>
    /// Start background job for wireless broadband report generation.
    /// Use this for large reports (region-level) that may take several minutes to generate.
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="filterByStatus">Optional filter by service status (Served=2, Underserved=1, Unserved=0). Only applicable when includeScores is true.</param>
    /// <param name="format">Report format: Excel (0) or Pdf (1). Default: Excel</param>
    /// <param name="includeScores">When true (default), includes connectivity scores and service status in the report.</param>
    /// <returns>Job information with status URL</returns>
    [HttpPost("wireless-broadband/generate-async")]
    [RequirePermission(Permissions.CreateReports)]
    public async Task<ActionResult<ApiResponse<WirelessBroadbandReportJobResponseDto>>> GenerateWirelessBroadbandReportAsync(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ServiceStatusEnum? filterByStatus = null,
        [FromQuery] ReportFormatEnum format = ReportFormatEnum.Excel,
        [FromQuery] bool includeScores = true)
    {
        var command = new StartWirelessBroadbandReportJobCommand
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            FilterByStatus = filterByStatus,
            Format = format,
            IncludeScores = includeScores
        };

        var jobId = await _mediator.Send(command);

        var response = new WirelessBroadbandReportJobResponseDto
        {
            JobId = jobId,
            Status = ReportJobStatusEnum.Queued,
            Message = "Report generation job has been queued successfully",
            EstimatedDurationMinutes = 2
        };

        return Ok(new ApiResponse<WirelessBroadbandReportJobResponseDto>
        {
            Data = response,
            Succeeded = true,
            Message = "Wireless broadband report job started successfully. Use the job ID to check status."
        });
    }

    /// <summary>
    /// Get status of wireless broadband report background job
    /// </summary>
    [HttpGet("wireless-broadband/job/{jobId}/status")]
    [RequirePermission(Permissions.ViewReports)]
    public ActionResult<ApiResponse<WirelessBroadbandReportJobStatusDto>> GetWirelessBroadbandReportJobStatus(string jobId)
    {
        var cacheKey = $"wireless_broadband_report_job_{jobId}";

        if (!_memoryCache.TryGetValue<WirelessBroadbandReportJobStatusDto>(cacheKey, out var statusDto) || statusDto == null)
        {
            return NotFound(new ApiResponse<WirelessBroadbandReportJobStatusDto>
            {
                Message = "Report job not found or has expired",
                Succeeded = false
            });
        }

        return Ok(new ApiResponse<WirelessBroadbandReportJobStatusDto>
        {
            Data = statusDto,
            Message = statusDto.StatusDescription,
            Succeeded = true
        });
    }

    /// <summary>
    /// Download completed wireless broadband report
    /// </summary>
    [HttpGet("wireless-broadband/job/{jobId}/download")]
    [RequirePermission(Permissions.ExportReports)]
    public async Task<ActionResult> DownloadWirelessBroadbandReport(string jobId)
    {
        var cacheKey = $"wireless_broadband_report_job_{jobId}";

        if (!_memoryCache.TryGetValue<WirelessBroadbandReportJobStatusDto>(cacheKey, out var statusDto) || statusDto == null)
        {
            return NotFound(new { message = "Report job not found or has expired" });
        }

        if (statusDto.Status != ReportJobStatusEnum.Completed)
        {
            return BadRequest(new
            {
                message = "Report is not ready for download",
                status = statusDto.Status.ToString(),
                currentStep = statusDto.CurrentStep
            });
        }

        if (string.IsNullOrEmpty(statusDto.FilePath))
        {
            return NotFound(new { message = "Report file path not found" });
        }

        // Download file from blob storage
        var containerName = "wireless-broadband-reports";
        var blobFileName = Path.GetFileName(statusDto.FilePath);
        var fileStream = await _blobStorageService.DownloadAsync(containerName, blobFileName);

        if (fileStream == null)
        {
            return NotFound(new { message = "Report file not found in storage" });
        }

        // Determine content type from format
        var contentType = statusDto.Format == ReportFormatEnum.Pdf
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(fileStream, contentType, blobFileName);
    }

    /// <summary>
    /// Generate a comprehensive wired broadband coverage report for municipalities and barangays.
    /// Shows service provider availability (PLDT, Globe, Converge, etc.) and service status.
    /// Supports pagination to handle large datasets efficiently.
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="filterByStatus">Optional filter by connectivity status (Served=2, Underserved=1, Unserved=0)</param>
    /// <param name="forceGenerateConnectivityAssessment">If true, forces generation of new connectivity assessments even if recent ones exist</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated wired broadband coverage report with municipality/barangay breakdown</returns>
    [HttpGet("wired-broadband")]
    [RequirePermission(Permissions.ViewReports)]
    public async Task<ActionResult<ApiResponse<WiredBroadbandReportDto>>> GetWiredBroadbandReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ServiceStatusEnum? filterByStatus = null,
        [FromQuery] bool forceGenerateConnectivityAssessment = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var request = new GetWiredBroadbandReportQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            FilterByStatus = filterByStatus,
            ForceGenerateConnectivityAssessment = forceGenerateConnectivityAssessment,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var report = await _mediator.Send(request);
        return report.Succeeded ? Ok(report) : BadRequest(report);
    }

    /// <summary>
    /// Generate and download wired broadband coverage report in PDF or Excel format
    /// </summary>
    /// <param name="format">Report format: Excel (0) or Pdf (1). Default: Excel</param>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="filterByStatus">Optional filter by connectivity status (Served=2, Underserved=1, Unserved=0). Default includes all statuses.</param>
    /// <param name="reportTitle">Optional custom title for the report. If not provided, uses location-based default title.</param>
    /// <param name="includeRemarks">When false (default), excludes the Remarks column in the report. Set to true to include remarks.</param>
    /// <returns>PDF or Excel file download</returns>
    [HttpGet("wired-broadband/download")]
    [RequirePermission(Permissions.ExportReports)]
    public async Task<ActionResult> DownloadWiredBroadbandReport(
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ServiceStatusEnum? filterByStatus = null,
        [FromQuery] string? reportTitle = null,
        [FromQuery] bool includeRemarks = false)
    {

        // Generate location-based default title if not provided
        var defaultTitle = await GenerateLocationBasedTitleAsync(
            "Wired Broadband Coverage Report",
            regionId, 
            provinceId, 
            municipalityId);
        
        var title = SanitizeReportTitle(reportTitle, defaultTitle);
        var fileName = await GenerateFileNameAsync("WiredBroadband", 
            format == ReportFormat.Pdf ? "pdf" : "xlsx", 
            regionId, 
            provinceId, 
            municipalityId);

        if (format == ReportFormat.Pdf)
        {
            var pdfBytes = await _connectivityReportingService.GenerateWiredBroadbandPdfAsync(
                regionId, provinceId, municipalityId, filterByStatus, title, includeRemarks);
            return File(pdfBytes, "application/pdf", fileName);
        }
        else
        {
            var excelBytes = await _connectivityReportingService.GenerateWiredBroadbandExcelAsync(
                regionId, provinceId, municipalityId, filterByStatus, title, includeRemarks);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }

    /// <summary>
    /// Generate connectivity gap analysis report showing unserved and underserved areas
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by Municipality ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Connectivity gap analysis report</returns>
    [HttpGet("connectivity-gap")]
    [RequirePermission(Permissions.ViewReports)]
    public async Task<ActionResult<ApiResponse<ConnectivityGapReportDto>>> GetConnectivityGapReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new Application.Features.Reports.ConnectivityGap.Requests.GetConnectivityGapReportQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Generate and download connectivity gap analysis report in PDF or Excel format.
    /// Includes all unserved and underserved areas with complete data (no samples or limits).
    /// </summary>
    /// <param name="format">Report format: Excel (0) or Pdf (1). Default: Excel</param>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by Municipality ID</param>
    /// <param name="reportTitle">Optional custom title for the report. If not provided, uses location-based default title.</param>
    /// <returns>PDF or Excel file download</returns>
    [HttpGet("connectivity-gap/download")]
    [RequirePermission(Permissions.ExportReports)]
    public async Task<ActionResult> DownloadConnectivityGapReport(
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] string? reportTitle = null)
    {
        var query = new Application.Features.Reports.ConnectivityGap.Requests.DownloadConnectivityGapReportQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            Format = format,
            ReportTitle = reportTitle
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded || result.Data == null)
            return BadRequest(result);

        return File(
            result.Data.FileContent,
            result.Data.ContentType,
            result.Data.FileName);
    }

    /// <summary>
    /// Generate connectivity report (served areas) showing areas with connectivity scores
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by Municipality ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Connectivity report with served areas</returns>
    [HttpGet("connectivity")]
    [RequirePermission(Permissions.ViewReports)]
    public async Task<ActionResult<ApiResponse<ConnectivityReportDto>>> GetConnectivityReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new Application.Features.Reports.Connectivity.Requests.GetConnectivityReportQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Generate and download connectivity report (served areas) in PDF or Excel format.
    /// Includes all served areas with complete data (no samples or limits). 
    /// Complete rubric score breakdown is included for all areas.
    /// </summary>
    /// <param name="format">Report format: Excel (0) or Pdf (1). Default: Excel</param>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by Municipality ID</param>
    /// <param name="reportTitle">Optional custom title for the report. If not provided, uses location-based default title.</param>
    /// <returns>PDF or Excel file download</returns>
    [HttpGet("connectivity/download")]
    [RequirePermission(Permissions.ExportReports)]
    public async Task<ActionResult> DownloadConnectivityReport(
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] string? reportTitle = null)
    {
        var query = new Application.Features.Reports.Connectivity.Requests.DownloadConnectivityReportQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            Format = format,
            ReportTitle = reportTitle
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded || result.Data == null)
            return BadRequest(result);

        return File(
            result.Data.FileContent,
            result.Data.ContentType,
            result.Data.FileName);
    }

    /// <summary>
    /// Generate a location-based default title for reports
    /// </summary>
    private async Task<string> GenerateLocationBasedTitleAsync(
        string baseTitle,
        Guid? regionId = null, 
        Guid? provinceId = null, 
        Guid? municipalityId = null)
    {
        try
        {
            if (municipalityId.HasValue)
            {
                var municipalityQuery = _municipalityRepository.Query(m => m.Id == municipalityId.Value);
                var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery);
                
                if (municipality != null)
                {
                    return $"{baseTitle} - {municipality.Name}";
                }
            }
            else if (provinceId.HasValue)
            {
                var provinceQuery = _provinceRepository.Query(p => p.Id == provinceId.Value);
                var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery);
                
                if (province != null)
                {
                    return $"{baseTitle} - {province.Name}";
                }
            }
            else if (regionId.HasValue)
            {
                var regionQuery = _regionRepository.Query(r => r.Id == regionId.Value);
                var region = await _regionRepository.FirstOrDefaultAsync(regionQuery);
                
                if (region != null)
                {
                    return $"{baseTitle} - {region.Name}";
                }
            }
        }
        catch (Exception)
        {
            // Fallback to base title if there's any error fetching names
        }
        
        return $"{baseTitle} - National";
    }

    

    /// <summary>
    /// Generate a descriptive filename for downloaded reports using location names instead of UUIDs
    /// </summary>
    private async Task<string> GenerateFileNameAsync(string reportType, string extension, 
        Guid? regionId = null, Guid? provinceId = null, Guid? municipalityId = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var scope = "National";
        
        try
        {
            if (municipalityId.HasValue)
            {
                var municipalityQuery = _municipalityRepository.Query(m => m.Id == municipalityId.Value);
                var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery);
                
                scope = municipality != null 
                    ? $"Municipality_{SanitizeFileName(municipality.Name)}"
                    : $"Municipality_{municipalityId}";
            }
            else if (provinceId.HasValue)
            {
                var provinceQuery = _provinceRepository.Query(p => p.Id == provinceId.Value);
                var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery);
                
                scope = province != null 
                    ? $"Province_{SanitizeFileName(province.Name)}"
                    : $"Province_{provinceId}";
            }
            else if (regionId.HasValue)
            {
                var regionQuery = _regionRepository.Query(r => r.Id == regionId.Value);
                var region = await _regionRepository.FirstOrDefaultAsync(regionQuery);
                
                scope = region != null 
                    ? $"Region_{SanitizeFileName(region.Name)}"
                    : $"Region_{regionId}";
            }
        }
        catch (Exception)
        {
            // Fallback to UUID-based naming if there's any error fetching names
            if (municipalityId.HasValue)
                scope = $"Municipality_{municipalityId}";
            else if (provinceId.HasValue)
                scope = $"Province_{provinceId}";
            else if (regionId.HasValue)
                scope = $"Region_{regionId}";
        }
        
        return $"{reportType}Report_{scope}_{timestamp}.{extension}";
    }

    /// <summary>
    /// Sanitize and validate report title from user input
    /// </summary>
    private static string SanitizeReportTitle(string? title, string defaultTitle)
    {
        // Use default if null or whitespace
        if (string.IsNullOrWhiteSpace(title))
            return defaultTitle;
        
        // Trim and limit length to prevent excessively long titles
        var sanitized = title.Trim();
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..100];
        }
        
        // Remove or replace potentially dangerous characters
        // Allow alphanumeric, spaces, hyphens, parentheses, and basic punctuation
        var allowedChars = sanitized.Where(c => 
            char.IsLetterOrDigit(c) || 
            char.IsWhiteSpace(c) || 
            c == '-' || c == '_' || c == '(' || c == ')' || 
            c == '.' || c == ',' || c == ':');
        
        sanitized = new string(allowedChars.ToArray());
        
        // Remove excessive whitespace
        while (sanitized.Contains("  "))
        {
            sanitized = sanitized.Replace("  ", " ");
        }
        
        sanitized = sanitized.Trim();
        
        // Return default if sanitization results in empty string
        return string.IsNullOrWhiteSpace(sanitized) ? defaultTitle : sanitized;
    }

    /// <summary>
    /// Sanitize a location name to make it safe for use in filenames
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Unknown";

        // Replace invalid filename characters with underscores
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = name;
        
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }
        
        // Replace spaces with underscores and remove consecutive underscores
        sanitized = sanitized.Replace(' ', '_');
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }
        
        // Trim underscores and dots from start and end (prevents hidden files and relative paths)
        sanitized = sanitized.Trim('_', '.');
        
        // Check for Windows reserved names (CON, PRN, AUX, NUL, COM1-9, LPT1-9)
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", 
                                     "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", 
                                     "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        
        if (reservedNames.Contains(sanitized.ToUpperInvariant()))
        {
            sanitized = $"{sanitized}_file";
        }
        
        // Limit length to prevent excessively long filenames
        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50].TrimEnd('_', '.');
        }
        
        return string.IsNullOrEmpty(sanitized) ? "Unknown" : sanitized;
    }

    /// <summary>
    /// Generate wired broadband report as a background job (recommended for large reports)
    /// </summary>
    /// <param name="format">Report format: Excel (0) or Pdf (1)</param>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="filterByStatus">Optional filter by connectivity status (Served=2, Underserved=1, Unserved=0)</param>
    /// <param name="reportTitle">Optional custom title for the report</param>
    /// <param name="includeRemarks">When false (default), excludes the Remarks column in the report</param>
    /// <param name="forceGenerateConnectivityAssessment">When true, forces generation of new connectivity assessments</param>
    /// <returns>Job information with status URL</returns>
    [HttpPost("wired-broadband/generate-async")]
    [RequirePermission(Permissions.CreateReports)]
    public Task<ActionResult<ApiResponse<WiredBroadbandReportJobResponseDto>>> GenerateWiredBroadbandReportAsync(
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ServiceStatusEnum? filterByStatus = null,
        [FromQuery] string? reportTitle = null,
        [FromQuery] bool includeRemarks = false,
        [FromQuery] bool forceGenerateConnectivityAssessment = false)
    {
        try
        {
            // Get user ID from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            // Generate unique job ID before enqueueing (Hangfire pattern)
            var jobId = Guid.NewGuid().ToString("N");

            // Create job request with pre-generated job ID
            var jobRequest = new WiredBroadbandReportJobRequestDto
            {
                JobId = jobId,
                UserId = userId,
                RegionId = regionId,
                ProvinceId = provinceId,
                MunicipalityId = municipalityId,
                FilterByStatus = filterByStatus,
                Format = format,
                ReportTitle = reportTitle,
                IncludeRemarks = includeRemarks,
                ForceGenerateConnectivityAssessment = forceGenerateConnectivityAssessment
            };

            // Enqueue background job (Hangfire may return a different ID, but we use our generated one)
            _backgroundJobClient.Enqueue<IProcessWiredBroadbandReportJob>(
                job => job.ExecuteAsync(jobRequest, CancellationToken.None));

            // Initialize job status in cache
            var statusDto = new WiredBroadbandReportJobStatusDto
            {
                JobId = jobId,
                Status = WiredBroadbandReportJobStatusEnum.Pending,
                Message = "Job queued for processing",
                LastUpdated = DateTime.UtcNow,
                Format = format
            };

            var cacheKey = $"wired_broadband_report_job_{jobId}";
            _memoryCache.Set(cacheKey, statusDto, TimeSpan.FromHours(1));

            // Create response
            var response = new WiredBroadbandReportJobResponseDto
            {
                JobId = jobId,
                Status = WiredBroadbandReportJobStatusEnum.Pending,
                Message = "Report generation job has been queued successfully",
                EstimatedDurationMinutes = 2, // Estimate based on typical report size
                DownloadUrl = null // Will be available after job completes
            };

            return Task.FromResult<ActionResult<ApiResponse<WiredBroadbandReportJobResponseDto>>>(Ok(new ApiResponse<WiredBroadbandReportJobResponseDto>
            {
                Data = response,
                Message = "Report generation job queued successfully",
                Succeeded = true
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult<ActionResult<ApiResponse<WiredBroadbandReportJobResponseDto>>>(BadRequest(new ApiResponse<WiredBroadbandReportJobResponseDto>
            {
                Message = $"Failed to queue report generation job: {ex.Message}",
                Succeeded = false
            }));
        }
    }

    /// <summary>
    /// Get the status of a wired broadband report generation job
    /// </summary>
    /// <param name="jobId">Job ID returned from generate-async endpoint</param>
    /// <returns>Job status with progress information</returns>
    [HttpGet("wired-broadband/job/{jobId}/status")]
    [RequirePermission(Permissions.ViewReports)]
    public ActionResult<ApiResponse<WiredBroadbandReportJobStatusDto>> GetWiredBroadbandReportJobStatus(string jobId)
    {
        try
        {
            var cacheKey = $"wired_broadband_report_job_{jobId}";
            
            if (!_memoryCache.TryGetValue(cacheKey, out WiredBroadbandReportJobStatusDto? statusDto) || statusDto == null)
            {
                return NotFound(new ApiResponse<WiredBroadbandReportJobStatusDto>
                {
                    Message = "Job not found or has expired",
                    Succeeded = false
                });
            }

            return Ok(new ApiResponse<WiredBroadbandReportJobStatusDto>
            {
                Data = statusDto,
                Message = statusDto.StatusDescription,
                Succeeded = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<WiredBroadbandReportJobStatusDto>
            {
                Message = $"Failed to retrieve job status: {ex.Message}",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Download a generated wired broadband report
    /// </summary>
    /// <param name="jobId">Job ID returned from generate-async endpoint</param>
    /// <returns>Report file download</returns>
    [HttpGet("wired-broadband/job/{jobId}/download")]
    [RequirePermission(Permissions.ExportReports)]
    public async Task<ActionResult> DownloadWiredBroadbandReportFromJob(string jobId)
    {
        try
        {
            var cacheKey = $"wired_broadband_report_job_{jobId}";
            
            if (!_memoryCache.TryGetValue(cacheKey, out WiredBroadbandReportJobStatusDto? statusDto) || statusDto == null)
            {
                return NotFound(new { message = "Job not found or has expired" });
            }

            if (statusDto.Status != WiredBroadbandReportJobStatusEnum.Completed)
            {
                return BadRequest(new { 
                    message = "Report is not ready for download", 
                    status = statusDto.Status.ToString(),
                    currentStep = statusDto.CurrentStep
                });
            }

            if (string.IsNullOrEmpty(statusDto.FilePath))
            {
                return NotFound(new { message = "Report file path not found" });
            }

            // Download file from blob storage
            var containerName = "wired-broadband-reports";
            var blobFileName = Path.GetFileName(statusDto.FilePath);
            var fileStream = await _blobStorageService.DownloadAsync(containerName, blobFileName);

            if (fileStream == null)
            {
                return NotFound(new { message = "Report file not found in storage" });
            }

            // Determine content type and file name from format
            var contentType = statusDto.Format == ReportFormat.Pdf
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(fileStream, contentType, blobFileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Failed to download report: {ex.Message}" });
        }
    }

    // ============================================================================
    // CONNECTIVITY GAP REPORT - ASYNC JOB-BASED ENDPOINTS
    // ============================================================================

    /// <summary>
    /// Generate a connectivity gap report asynchronously as a background job
    /// </summary>
    /// <param name="regionId">Optional filter by region</param>
    /// <param name="provinceId">Optional filter by province</param>
    /// <param name="municipalityId">Optional filter by municipality</param>
    /// <param name="format">Report format (Excel or PDF)</param>
    /// <param name="reportTitle">Optional custom report title</param>
    /// <returns>Job information including job ID for status tracking</returns>
    [HttpPost("connectivity-gap/generate-async")]
    [RequirePermission(Permissions.CreateReports)]
    public Task<ActionResult<ApiResponse<ConnectivityGapReportJobResponseDto>>> GenerateConnectivityGapReportAsync(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] string? reportTitle = null)
    {
        try
        {
            // Get current user ID
            var userId = User.Identity?.Name ?? "Anonymous";

            // Generate unique job ID
            var jobId = Guid.NewGuid().ToString();

            var jobRequest = new ConnectivityGapReportJobRequestDto
            {
                JobId = jobId,
                UserId = userId,
                RegionId = regionId,
                ProvinceId = provinceId,
                MunicipalityId = municipalityId,
                Format = format,
                ReportTitle = reportTitle
            };

            // Validate request
            var validator = new ConnectivityGapReportJobRequestDtoValidator();
            var validationResult = validator.Validate(jobRequest);
            if (!validationResult.IsValid)
            {
                return Task.FromResult<ActionResult<ApiResponse<ConnectivityGapReportJobResponseDto>>>(BadRequest(new ApiResponse<ConnectivityGapReportJobResponseDto>
                {
                    Succeeded = false,
                    Message = "Validation failed",
                    Errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }));
            }

            // Enqueue background job
            _backgroundJobClient.Enqueue<IProcessConnectivityGapReportJob>(
                job => job.ExecuteAsync(jobRequest, CancellationToken.None));

            var statusDto = new ConnectivityGapReportJobStatusDto
            {
                JobId = jobId,
                Status = ConnectivityGapReportJobStatusEnum.Pending,
                Message = "Report generation job has been queued",
                LastUpdated = DateTime.UtcNow
            };

            // Cache initial status
            var cacheKey = $"connectivity_gap_report_job_{jobId}";
            _memoryCache.Set(cacheKey, statusDto, TimeSpan.FromHours(1));

            var response = new ConnectivityGapReportJobResponseDto
            {
                JobId = jobId,
                Status = ConnectivityGapReportJobStatusEnum.Pending,
                Message = "Report generation job has been queued successfully",
                EstimatedDurationMinutes = 2
            };

            return Task.FromResult<ActionResult<ApiResponse<ConnectivityGapReportJobResponseDto>>>(Ok(new ApiResponse<ConnectivityGapReportJobResponseDto>
            {
                Succeeded = true,
                Data = response,
                Message = "Report generation job started successfully"
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult<ActionResult<ApiResponse<ConnectivityGapReportJobResponseDto>>>(BadRequest(new ApiResponse<ConnectivityGapReportJobResponseDto>
            {
                Succeeded = false,
                Message = $"Failed to start report generation: {ex.Message}"
            }));
        }
    }

    /// <summary>
    /// Get the status of a connectivity gap report generation job
    /// </summary>
    /// <param name="jobId">Job ID returned from generate-async endpoint</param>
    /// <returns>Current job status with progress information</returns>
    [HttpGet("connectivity-gap/job/{jobId}/status")]
    [RequirePermission(Permissions.ViewReports)]
    public ActionResult<ApiResponse<ConnectivityGapReportJobStatusDto>> GetConnectivityGapReportJobStatus(string jobId)
    {
        try
        {
            var cacheKey = $"connectivity_gap_report_job_{jobId}";
            
            if (!_memoryCache.TryGetValue(cacheKey, out ConnectivityGapReportJobStatusDto? statusDto) || statusDto == null)
            {
                return NotFound(new ApiResponse<ConnectivityGapReportJobStatusDto>
                {
                    Succeeded = false,
                    Message = "Job not found or has expired"
                });
            }

            return Ok(new ApiResponse<ConnectivityGapReportJobStatusDto>
            {
                Succeeded = true,
                Data = statusDto,
                Message = "Job status retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<ConnectivityGapReportJobStatusDto>
            {
                Succeeded = false,
                Message = $"Failed to retrieve job status: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Download a generated connectivity gap report
    /// </summary>
    /// <param name="jobId">Job ID returned from generate-async endpoint</param>
    /// <returns>Report file download</returns>
    [HttpGet("connectivity-gap/job/{jobId}/download")]
    [RequirePermission(Permissions.ExportReports)]
    public async Task<ActionResult> DownloadConnectivityGapReportFromJob(string jobId)
    {
        try
        {
            var cacheKey = $"connectivity_gap_report_job_{jobId}";
            
            if (!_memoryCache.TryGetValue(cacheKey, out ConnectivityGapReportJobStatusDto? statusDto) || statusDto == null)
            {
                return NotFound(new { message = "Job not found or has expired" });
            }

            if (statusDto.Status != ConnectivityGapReportJobStatusEnum.Completed)
            {
                return BadRequest(new { 
                    message = "Report is not ready for download", 
                    status = statusDto.Status.ToString(),
                    currentStep = statusDto.CurrentStep
                });
            }

            if (string.IsNullOrEmpty(statusDto.FilePath))
            {
                return NotFound(new { message = "Report file path not found" });
            }

            // Download file from blob storage
            var containerName = "connectivity-gap-reports";
            var blobFileName = Path.GetFileName(statusDto.FilePath);
            var fileStream = await _blobStorageService.DownloadAsync(containerName, blobFileName);

            if (fileStream == null)
            {
                return NotFound(new { message = "Report file not found in storage" });
            }

            // Determine content type and file name from format
            var contentType = statusDto.Format == ReportFormat.Pdf
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(fileStream, contentType, blobFileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Failed to download report: {ex.Message}" });
        }
    }

    // ============================================================================
    // CONNECTIVITY REPORT - ASYNC JOB-BASED ENDPOINTS (SERVED AREAS)
    // ============================================================================

    /// <summary>
    /// Generate a connectivity report (served areas) asynchronously as a background job
    /// </summary>
    /// <param name="regionId">Optional filter by region</param>
    /// <param name="provinceId">Optional filter by province</param>
    /// <param name="municipalityId">Optional filter by municipality</param>
    /// <param name="format">Report format (Excel or PDF)</param>
    /// <param name="reportTitle">Optional custom report title</param>
    /// <returns>Job information including job ID for status tracking</returns>
    [HttpPost("connectivity/generate-async")]
    [RequirePermission(Permissions.CreateReports)]
    public Task<ActionResult<ApiResponse<ConnectivityReportJobResponseDto>>> GenerateConnectivityReportAsync(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] ReportFormat format = ReportFormat.Excel,
        [FromQuery] string? reportTitle = null)
    {
        try
        {
            // Get current user ID
            var userId = User.Identity?.Name ?? "Anonymous";

            // Generate unique job ID
            var jobId = Guid.NewGuid().ToString();

            var jobRequest = new ConnectivityReportJobRequestDto
            {
                JobId = jobId,
                UserId = userId,
                RegionId = regionId,
                ProvinceId = provinceId,
                MunicipalityId = municipalityId,
                Format = format,
                ReportTitle = reportTitle
            };

            // Validate request
            var validator = new ConnectivityReportJobRequestDtoValidator();
            var validationResult = validator.Validate(jobRequest);
            if (!validationResult.IsValid)
            {
                return Task.FromResult<ActionResult<ApiResponse<ConnectivityReportJobResponseDto>>>(BadRequest(new ApiResponse<ConnectivityReportJobResponseDto>
                {
                    Succeeded = false,
                    Message = "Validation failed",
                    Errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }));
            }

            // Enqueue background job
            _backgroundJobClient.Enqueue<IProcessConnectivityReportJob>(
                job => job.ExecuteAsync(jobRequest, CancellationToken.None));

            var statusDto = new ConnectivityReportJobStatusDto
            {
                JobId = jobId,
                Status = ConnectivityReportJobStatusEnum.Pending,
                Message = "Report generation job has been queued",
                LastUpdated = DateTime.UtcNow
            };

            // Cache initial status
            var cacheKey = $"connectivity_report_job_{jobId}";
            _memoryCache.Set(cacheKey, statusDto, TimeSpan.FromHours(1));

            var response = new ConnectivityReportJobResponseDto
            {
                JobId = jobId,
                Status = ConnectivityReportJobStatusEnum.Pending,
                Message = "Report generation job has been queued successfully",
                EstimatedDurationMinutes = 2
            };

            return Task.FromResult<ActionResult<ApiResponse<ConnectivityReportJobResponseDto>>>(Ok(new ApiResponse<ConnectivityReportJobResponseDto>
            {
                Succeeded = true,
                Data = response,
                Message = "Report generation job started successfully"
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult<ActionResult<ApiResponse<ConnectivityReportJobResponseDto>>>(BadRequest(new ApiResponse<ConnectivityReportJobResponseDto>
            {
                Succeeded = false,
                Message = $"Failed to start report generation: {ex.Message}"
            }));
        }
    }

    /// <summary>
    /// Get the status of a connectivity report generation job
    /// </summary>
    /// <param name="jobId">Job ID returned from generate-async endpoint</param>
    /// <returns>Current job status with progress information</returns>
    [HttpGet("connectivity/job/{jobId}/status")]
    [RequirePermission(Permissions.ViewReports)]
    public ActionResult<ApiResponse<ConnectivityReportJobStatusDto>> GetConnectivityReportJobStatus(string jobId)
    {
        try
        {
            var cacheKey = $"connectivity_report_job_{jobId}";
            
            if (!_memoryCache.TryGetValue(cacheKey, out ConnectivityReportJobStatusDto? statusDto) || statusDto == null)
            {
                return NotFound(new ApiResponse<ConnectivityReportJobStatusDto>
                {
                    Succeeded = false,
                    Message = "Job not found or has expired"
                });
            }

            return Ok(new ApiResponse<ConnectivityReportJobStatusDto>
            {
                Succeeded = true,
                Data = statusDto,
                Message = "Job status retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<ConnectivityReportJobStatusDto>
            {
                Succeeded = false,
                Message = $"Failed to retrieve job status: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Download a generated connectivity report
    /// </summary>
    /// <param name="jobId">Job ID returned from generate-async endpoint</param>
    /// <returns>Report file download</returns>
    [HttpGet("connectivity/job/{jobId}/download")]
    [RequirePermission(Permissions.ExportReports)]
    public async Task<ActionResult> DownloadConnectivityReportFromJob(string jobId)
    {
        try
        {
            var cacheKey = $"connectivity_report_job_{jobId}";
            
            if (!_memoryCache.TryGetValue(cacheKey, out ConnectivityReportJobStatusDto? statusDto) || statusDto == null)
            {
                return NotFound(new { message = "Job not found or has expired" });
            }

            if (statusDto.Status != ConnectivityReportJobStatusEnum.Completed)
            {
                return BadRequest(new { 
                    message = "Report is not ready for download", 
                    status = statusDto.Status.ToString(),
                    currentStep = statusDto.CurrentStep
                });
            }

            if (string.IsNullOrEmpty(statusDto.FilePath))
            {
                return NotFound(new { message = "Report file path not found" });
            }

            // Download file from blob storage
            var containerName = "connectivity-reports";
            var blobFileName = Path.GetFileName(statusDto.FilePath);
            var fileStream = await _blobStorageService.DownloadAsync(containerName, blobFileName);

            if (fileStream == null)
            {
                return NotFound(new { message = "Report file not found in storage" });
            }

            // Determine content type and file name from format
            var contentType = statusDto.Format == ReportFormat.Pdf
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(fileStream, contentType, blobFileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Failed to download report: {ex.Message}" });
        }
    }
}

