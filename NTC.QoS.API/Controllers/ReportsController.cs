using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("reports", Name = "reports")]
[Authorize]
public class ReportsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("wireless-broadband")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<WirelessBroadbandReportResult>), 400)]
    public async Task<IActionResult> GetWirelessBroadbandReport([FromQuery] GetWirelessBroadbandReportQuery query)
    {
        var result = await _mediator.Send(query);
        
        if (result.Succeeded && result.Data != null)
        {
            return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
        }
        
        return BadRequest(result);
    }
}
