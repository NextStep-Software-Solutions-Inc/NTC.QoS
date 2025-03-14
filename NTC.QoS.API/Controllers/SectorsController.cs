using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("sectors", Name= "sectors")]
public class SectorsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreateSector(CreateSectorDto sectorDto)
    {
        var sector = await _mediator.Send(new CreateSectorRequest(sectorDto));
        return sector.Succeeded ? Ok(sector) : BadRequest(sector);
    }
}
