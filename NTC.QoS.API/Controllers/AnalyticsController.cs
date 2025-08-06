using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("analytics", Name = "analytics")]
public class AnalyticsController(IMediator mediator) : ControllerBase
{
    [HttpPost("area-broadband-info")]
    public async Task<ActionResult<ApiResponse<AreaBroadbandInfoDto>>> GetAreaBroadbandInfo([FromBody] BoundingBoxDto boundingBox)
    {
        var result = await mediator.Send(new GetAreaBroadbandInfoRequest { BoundingBox = boundingBox });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
