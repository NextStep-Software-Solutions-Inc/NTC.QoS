using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("api/reports", Name = "reports")]
[Authorize]
public class ReportsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

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
}