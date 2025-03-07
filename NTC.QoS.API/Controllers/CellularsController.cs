using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cellulars", Name = "cellulars")]
public class CellularsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreateCellular(CreateCellularDto cellularDto)
    {
        var cellular = await _mediator.Send(new CreateCellularRequest
        {
            Cellular = cellularDto
        });
        return cellular.Succeeded ? Ok(cellular) : BadRequest(cellular);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GetCellularDto>>> GetAllCellulars(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllCellularsRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber
        });
        return Ok(result);
    }

}
