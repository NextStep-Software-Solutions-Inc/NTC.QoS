using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("dashboard", Name = "dashboard")]
public class DashboardController(IMediator mediator, ILogger<DashboardController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<DashboardController> _logger = logger;

    [HttpGet("cell-counts")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalCellsitesCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        var request = new GetTotalCellsitesCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("NAPs-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalNAPsCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        var request = new GetTotalNAPsCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("regions/top-by-cell-count")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopRegionsByCellCount([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? daysInPast, [FromQuery] int limit = 0)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        var query = new GetTopRegionsByCellCountRequest { Limit = limit, StartDate = finalStartDate, EndDate = finalEndDate };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("provinces/top-by-cell-count/region-id")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopProvincesInRegionByCellCount([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? daysInPast, [FromQuery] Guid regionId, [FromQuery] int limit = 0)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        var query = new GetTopProvincesInRegionByCellCountRequest { StartDate = finalStartDate, EndDate = finalEndDate, RegionId = regionId, Limit = limit };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetDashboardOverview(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast,
        [FromQuery] Guid? regionId,
        [FromQuery] Guid? provinceId,
        [FromQuery] Guid? municipalityId,
        
        [FromQuery] int limit = 5)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        var totalCellCounts = new GetTotalCellsitesCountRequest();

        var request = new GetDashboardOverviewRequest { StartDate = finalStartDate, EndDate = finalEndDate, RegionId = regionId, ProvinceId = provinceId, MunicipalityId = municipalityId, Limit = limit };
        var response = await _mediator.Send(request);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }
}
