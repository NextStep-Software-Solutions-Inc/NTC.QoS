using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cellulars", Name = "cellulars")]
public class CellularsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CellularsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCellular(CreateCellularDto cellularDto)
    {
        var cellular = await _mediator.Send(new CreateCellularRequest
        {
            Cellular = cellularDto
        });
        return cellular.Succeeded ? Ok(cellular) : BadRequest(cellular);
    }
}
