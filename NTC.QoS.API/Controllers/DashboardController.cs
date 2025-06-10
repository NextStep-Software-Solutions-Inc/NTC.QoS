using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Models.DTOs;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("dashboard", Name = "dashboard")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("cell-counts")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalVerifiedCellsitesCount(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate)
    {
        DateTime utcStartDate = startDate.ToUniversalTime();
        DateTime utcEndDate = endDate.ToUniversalTime();
        var request = new GetTotalCellsitesCountRequest { StartDate = utcStartDate, EndDate = utcEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("NAPs-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalNAPsCount(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate)
    {
        DateTime utcStartDate = startDate.ToUniversalTime();
        DateTime utcEndDate = endDate.ToUniversalTime();
        var request = new GetTotalNAPsCountRequest { StartDate = utcStartDate, EndDate = utcEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }
}
