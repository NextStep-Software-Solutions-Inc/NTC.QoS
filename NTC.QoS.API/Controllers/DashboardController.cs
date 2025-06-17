using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Models.DTOs.Dashboard_1;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("dashboards", Name = "dashboards")]
public class DashboardsController(IMediator mediator) : ControllerBase
{
    [HttpGet("top-provinces-by-cell-count")]
    public async Task<ActionResult<List<TopProvinceCellCountDto>>> GetTopProvincesByCellCount(
        [FromQuery] Guid regionId,
        [FromQuery][Range(1, 50)] int top = 5)
    {
        var request = new GetTopProvincesByCellCountQueryRequest
        {
            RegionId = regionId,
            Top = top
        };
        var result = await mediator.Send(request);
        return Ok(result);
    }
}
