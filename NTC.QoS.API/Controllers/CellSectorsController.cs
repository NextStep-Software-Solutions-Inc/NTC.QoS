using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cell-sectors", Name = "cell-sectors")]
public class CellSectorsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CellSectorDto>>> CreateCellSector(CreateCellSectorDto createCellSector)
    {
        var request = new CreateCellSectorRequest
        { 
            CellSector = createCellSector
        };
        var cellSector = await _mediator.Send(request);
        return cellSector.Succeeded ? Ok(cellSector) : BadRequest(cellSector);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CellSectorDto>>> GetAllCellSectors([FromQuery] int pageNumber = 1,
                                                                            [FromQuery] int pageSize = 10,
                                                                            [FromQuery] List<Guid>? cellIds = null)
    {
        var request = new GetAllCellSectorsRequest(pageNumber, pageSize)
        {
            CellIds = cellIds
        };
        var cellSectors = await _mediator.Send(request);
        return cellSectors.Succeeded ? Ok(cellSectors) : BadRequest(cellSectors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CellSectorDto>>> GetCellSectorById(Guid id)
    {
        var cellSector = await _mediator.Send(new GetCellSectorByIdRequest(id));
        return cellSector.Succeeded ? Ok(cellSector) : BadRequest(cellSector);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<CellSectorDto>>> UpdateCellSectorById(Guid id, UpdateCellSectorDto updateCellSector)
    {
        var request = new UpdateCellSectorByIdRequest
        {
            Id = id,
            CellSector = updateCellSector
        };
        var cellSector = await _mediator.Send(request);
        return cellSector.Succeeded ? Ok(cellSector) : BadRequest(cellSector);
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellSectorById(Guid id)
    {
        var cellSector = await _mediator.Send(new DeleteCellSectorByIdRequest(id));
        return cellSector.Succeeded ? Ok(cellSector) : BadRequest(cellSector);
    }
}
