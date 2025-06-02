using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cells", Name = "cells")]
public class CellsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CellDto>>> CreateCell(CreateCellDto createCell)
    {
        var cell = await _mediator.Send(new CreateCellRequest(createCell));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllCells([FromQuery] int pageNumber = 1,
                                                                            [FromQuery] int pageSize = 10,
                                                                            [FromQuery] string? search = null,
                                                                            [FromQuery] string? status = null,
                                                                            [FromQuery] List<Guid>? serviceProviderIds = null,
                                                                            [FromQuery] List<Guid>? providerIds = null,
                                                                            [FromQuery] List<Guid>? serviceIds = null,
                                                                            double longitude = 0,
                                                                            double latitude = 0,
                                                                            double distance = 1000)
    {
        var request = new GetAllCellsRequest(pageNumber, pageSize)
        {
            Search = search,
            Status = status,
            ServiceProviderIds = serviceProviderIds,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance
        };
        var cells = await _mediator.Send(request);
        return cells.Succeeded ? Ok(cells) : BadRequest(cells);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CellDto>>> GetCellById(Guid id)
    {
        var cell = await _mediator.Send(new GetCellByIdRequest(id));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateCellById(Guid id, UpdateCellDto updateCell)
    {
        var cell = await _mediator.Send(new UpdateCellByIdRequest(id, updateCell));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateStatusById(Guid id, [FromBody] string status)
    {
        var cell = await _mediator.Send(new UpdateCellStatusByIdRequest(id, status));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }
    
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellById([FromQuery] List<Guid> ids)
    {
        var cells = await _mediator.Send(new DeleteCellsByIdsRequest { Ids = ids});
        return cells.Succeeded ? Ok(cells) : BadRequest(cells);
    }
}
