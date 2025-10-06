using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("reports", Name = "reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConnectivityReportingService _connectivityReportingService;

    public ReportsController(IMediator mediator, IConnectivityReportingService connectivityReportingService)
    {
        _mediator = mediator;
        _connectivityReportingService = connectivityReportingService;
    }

    /// <summary>
    /// Generate a comprehensive wired broadband coverage report for municipalities and barangays.
    /// Shows service provider availability (PLDT, Globe, Converge, etc.) and service status.
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="startDate">Optional start date filter for infrastructure data</param>
    /// <param name="endDate">Optional end date filter for infrastructure data</param>
    /// <param name="includeOnlyServedAreas">If true, only include barangays with served status</param>
    /// <returns>Wired broadband coverage report with municipality/barangay breakdown</returns>
    [HttpGet("wired-broadband")]
    public async Task<ActionResult<ApiResponse<WiredBroadbandReportDto>>> GetWiredBroadbandReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeOnlyServedAreas = false)
    {
        var request = new GetWiredBroadbandReportQuery
        {
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            StartDate = startDate,
            EndDate = endDate,
            IncludeOnlyServedAreas = includeOnlyServedAreas
        };

        var report = await _mediator.Send(request);
        return report.Succeeded ? Ok(report) : BadRequest(report);
    }

    /// <summary>
    /// Generate and download wired broadband coverage report as PDF
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="startDate">Optional start date filter for infrastructure data</param>
    /// <param name="endDate">Optional end date filter for infrastructure data</param>
    /// <param name="includeOnlyServedAreas">If true, only include barangays with served status</param>
    /// <param name="reportTitle">Custom title for the report</param>
    /// <returns>PDF file download</returns>
    [HttpGet("wired-broadband/pdf")]
    public async Task<ActionResult> GetWiredBroadbandPdfReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeOnlyServedAreas = false,
        [FromQuery] string? reportTitle = null)
    {
        var title = reportTitle ?? "Wired Broadband Coverage Report";
        var pdfBytes = await _connectivityReportingService.GenerateWiredBroadbandPdfAsync(
            regionId, provinceId, municipalityId, startDate, endDate, includeOnlyServedAreas, title);

        var fileName = GenerateFileName("WiredBroadband", "pdf", regionId, provinceId, municipalityId);
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Generate and download wired broadband coverage report as Excel
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="municipalityId">Optional filter by specific Municipality ID</param>
    /// <param name="startDate">Optional start date filter for infrastructure data</param>
    /// <param name="endDate">Optional end date filter for infrastructure data</param>
    /// <param name="includeOnlyServedAreas">If true, only include barangays with served status</param>
    /// <param name="reportTitle">Custom title for the report</param>
    /// <returns>Excel file download</returns>
    [HttpGet("wired-broadband/excel")]
    public async Task<ActionResult> GetWiredBroadbandExcelReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeOnlyServedAreas = false,
        [FromQuery] string? reportTitle = null)
    {
        var title = reportTitle ?? "Wired Broadband Coverage Report";
        var excelBytes = await _connectivityReportingService.GenerateWiredBroadbandExcelAsync(
            regionId, provinceId, municipalityId, startDate, endDate, includeOnlyServedAreas, title);

        var fileName = GenerateFileName("WiredBroadband", "xlsx", regionId, provinceId, municipalityId);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Generate connectivity gap analysis report showing unserved and underserved areas
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <returns>Connectivity gap analysis report</returns>
    [HttpGet("connectivity-gap")]
    public async Task<ActionResult<ApiResponse<ConnectivityGapReportDto>>> GetConnectivityGapReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null)
    {
        try
        {
            var report = await _connectivityReportingService.GenerateConnectivityGapReportAsync(regionId, provinceId);
            
            return Ok(new ApiResponse<ConnectivityGapReportDto>
            {
                Data = report,
                Message = "Connectivity gap report generated successfully.",
                Succeeded = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<ConnectivityGapReportDto>
            {
                Message = $"Failed to generate connectivity gap report: {ex.Message}",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Generate and download connectivity gap analysis report as PDF
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="reportTitle">Custom title for the report</param>
    /// <returns>PDF file download</returns>
    [HttpGet("connectivity-gap/pdf")]
    public async Task<ActionResult> GetConnectivityGapPdfReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] string? reportTitle = null)
    {
        var title = reportTitle ?? "Connectivity Gap Analysis Report";
        var pdfBytes = await _connectivityReportingService.GenerateConnectivityGapPdfAsync(
            regionId, provinceId, title);

        var fileName = GenerateFileName("ConnectivityGap", "pdf", regionId, provinceId, null);
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Generate and download connectivity gap analysis report as Excel
    /// </summary>
    /// <param name="regionId">Optional filter by Region ID</param>
    /// <param name="provinceId">Optional filter by Province ID</param>
    /// <param name="reportTitle">Custom title for the report</param>
    /// <returns>Excel file download</returns>
    [HttpGet("connectivity-gap/excel")]
    public async Task<ActionResult> GetConnectivityGapExcelReport(
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] string? reportTitle = null)
    {
        var title = reportTitle ?? "Connectivity Gap Analysis Report";
        var excelBytes = await _connectivityReportingService.GenerateConnectivityGapExcelAsync(
            regionId, provinceId, title);

        var fileName = GenerateFileName("ConnectivityGap", "xlsx", regionId, provinceId, null);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Generate a descriptive filename for downloaded reports
    /// </summary>
    private static string GenerateFileName(string reportType, string extension, 
        Guid? regionId = null, Guid? provinceId = null, Guid? municipalityId = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var scope = "National";
        
        if (municipalityId.HasValue)
            scope = $"Municipality_{municipalityId}";
        else if (provinceId.HasValue)
            scope = $"Province_{provinceId}";
        else if (regionId.HasValue)
            scope = $"Region_{regionId}";
        
        return $"{reportType}Report_{scope}_{timestamp}.{extension}";
    }
}