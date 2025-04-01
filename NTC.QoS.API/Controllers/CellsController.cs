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
                                                                            [FromQuery] Guid? fileId = null,
                                                                            [FromQuery] Guid? zoneId = null,
                                                                            [FromQuery] Guid? serviceProviderId = null,
                                                                            [FromQuery] Guid? providerId = null,
                                                                            [FromQuery] Guid? serviceId = null)
    {
        var request = new GetAllCellsRequest(pageNumber, pageSize, search, status, fileId, zoneId, serviceProviderId, providerId, serviceId);
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
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateStatusById(Guid id, UpdateCellStatusDto updateCellStatusDto)
    {
        var cell = await _mediator.Send(new UpdateCellStatusByIdRequest(id, new UpdateCellStatusDto
        {
            Status = updateCellStatusDto.Status
        }));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellById(Guid id)
    {
        var cell = await _mediator.Send(new DeleteCellByIdRequest(id));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }
}
