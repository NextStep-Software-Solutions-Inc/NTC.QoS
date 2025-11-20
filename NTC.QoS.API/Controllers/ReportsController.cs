using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Reports.Queries.GetWirelessBroadbandReport;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("reports", Name = "reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("wireless-broadband/{municipalityId}")]
    [ProducesResponseType(typeof(ApiResponse<WirelessBroadbandReportDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<WirelessBroadbandReportDto>), 400)]
    public async Task<ActionResult<ApiResponse<WirelessBroadbandReportDto>>> GetWirelessBroadbandReport(Guid municipalityId)
    {
        var result = await _mediator.Send(new GetWirelessBroadbandReportQuery(municipalityId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
