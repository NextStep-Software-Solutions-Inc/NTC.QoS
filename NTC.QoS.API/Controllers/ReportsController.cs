using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Reports.Queries.GetWirelessBroadbandReport;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
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
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<WirelessBroadbandReportResult>), 400)]
    public async Task<IActionResult> GetWirelessBroadbandReport(Guid municipalityId)
    {
        var result = await _mediator.Send(new GetWirelessBroadbandReportQuery(municipalityId));
        
        if (result.Succeeded && result.Data != null)
        {
            return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
        }
        
        return BadRequest(result);
    }
}
