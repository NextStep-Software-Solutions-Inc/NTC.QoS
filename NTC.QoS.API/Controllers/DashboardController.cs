using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("dashboard", Name = "dashboard")]
public class DashboardController(IMediator mediator, IDateRangeHelper dateRangeHelper, ILogger<DashboardController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IDateRangeHelper _dateRangeHelper = dateRangeHelper;
    private readonly ILogger<DashboardController> _logger = logger;

    [HttpGet("cell-counts")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalCellsitesCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;

        if (startDate.HasValue && endDate.HasValue)
        {
            // Use provided dates, convert to UTC
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            // Use backend-driven default from the helper
            (finalStartDate, finalEndDate) = _dateRangeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (CellCounts): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate); ;
        }

        var request = new GetTotalCellsitesCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("NAPs-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalNAPsCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;

        if (startDate.HasValue && endDate.HasValue)
        {
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            (finalStartDate, finalEndDate) = _dateRangeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (NAPsCount): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }

        var request = new GetTotalNAPsCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("regions/top-by-cell-count")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopRegionsByCellCount([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int limit = 0)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;

        if (startDate.HasValue && endDate.HasValue)
        {
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            (finalStartDate, finalEndDate) = _dateRangeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (RegionsTopByCellCount): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }

        var query = new GetTopRegionsByCellCountRequest { Limit = limit, StartDate = finalStartDate, EndDate = finalEndDate };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("provinces/top-by-cell-count/region-id")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopProvincesInRegionByCellCount([Required][FromQuery] Guid regionId, [FromQuery] int limit = 0)
    {
        var query = new GetTopProvincesInRegionByCellCountRequest { RegionId = regionId, Limit = limit };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetDashboardOverview(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] int limit = 5)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;
        if (startDate.HasValue && endDate.HasValue)
        {
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            (finalStartDate, finalEndDate) = _dateRangeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (Overview): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }
        var request = new GetDashboardOverviewRequest { StartDate = finalStartDate, EndDate = finalEndDate, RegionId = regionId, ProvinceId = provinceId, MunicipalityId = municipalityId, Limit = limit };
        var response = await _mediator.Send(request);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }
}
