using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
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
    public async Task<ActionResult<PaginatedResponse<GetCellularDto>>> GetAllCellulars(int pageSize = 10,
                                                                                       int pageNumber = 1,
                                                                                       Guid? cellId = null,
                                                                                       Guid? serviceProviderId = null,
                                                                                       Guid? generationBandId = null)
    {
        var result = await _mediator.Send(new GetAllCellularsRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            CellId = cellId,
            ServiceProviderId = serviceProviderId,
            GenerationBandId = generationBandId,
        });
        return result.Succeeded ? Ok(result) : BadRequest(result);

    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CellularDto>>> GetCellularById(Guid id)
    {
        var request = new GetCellularByIdRequest(id);
        var cellular = await _mediator.Send(request);
        return cellular.Succeeded ? Ok(cellular) : BadRequest(cellular);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Cellular>>> DeleteCellularById(Guid id)
    {
        var cellular = await _mediator.Send(new DeleteCellularByIdRequest(id));
        return cellular.Succeeded ? Ok(cellular) : BadRequest(cellular);
    }
}
