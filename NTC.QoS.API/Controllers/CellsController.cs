using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cells", Name = "cells")]
public class CellsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CellsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CellDto>>>> GetAllCells([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var request = new GetAllCellsRequest(pageNumber, pageSize, search);
        var cells = await _mediator.Send(request);
        return cells.Succeeded ? Ok(cells) : BadRequest(cells);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CellDto>>> GetCellById(Guid id)
    {
        Guid cellId = id;
        var request = new GetCellByIdRequest(cellId);
        var cells = await _mediator.Send(request);
        return cells.Succeeded ? Ok(cells) : BadRequest(cells);
    }
    [HttpPut("update-status/{id}")]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateStatusById(Guid id, UpdateCellStatusDto updateCellStatusDto)
    {
        var cell = await _mediator.Send(new UpdateCellStatusByIdRequest(id, new UpdateCellStatusDto
        {
            Status = updateCellStatusDto.Status
        }));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Aggregates.Cell>>> UpdateCellById(Guid id, UpdateCellDto updateCell)
    {
        var cell = await _mediator.Send(new UpdateCellByIdRequest(id, new UpdateCellDto
        {
            Name = updateCell.Name,
            Description = updateCell.Description,
            SiteId = updateCell.SiteId,
            Group = updateCell.Group,
            FileId = updateCell.FileId,
            ZoneId = updateCell.ZoneId,
            ServiceProviderId = updateCell.ServiceProviderId,
            CID = updateCell.CID,
            TowerType = updateCell.TowerType,
            Elevation = updateCell.Elevation,
            CoorLatitude = updateCell.CoorLatitude,
            CoorLongitude = updateCell.CoorLongitude,
            LAC = updateCell.LAC,
            TAC = updateCell.TAC
        }));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellById(Guid id)
    {
        var cell = await _mediator.Send(new DeleteCellByIdRequest(id));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }
    [HttpPost]
    public async Task<IActionResult> Post(CreateCellDto createCellDto)
    {
        var cell = await _mediator.Send(new CreateCellRequest
        {
            CreateCell = createCellDto
        });

        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }
}
