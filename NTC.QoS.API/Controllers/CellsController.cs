using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cells", Name = "cells")]
public class CellsController(IMediator mediator, IMemoryCache memoryCache) : ControllerBase
{
    private const string CellsCacheKey = "cells";

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CellDto>>> CreateCell(CreateCellDto createCell)
    {
        var cell = await mediator.Send(new CreateCellRequest(createCell));
        if (!cell.Succeeded || cell.Data == null)
            return BadRequest(cell.Data == null ? "Cell creation failed, no data returned." : cell);

        memoryCache.Remove(CellsCacheKey);
        return CreatedAtAction(nameof(GetCellById), new { id = cell.Data.Id }, cell);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)] // tell browser to cache for 5 min
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllCells(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<Guid>? serviceIds = null,
        double longitude = 0,
        double latitude = 0,
        double distance = 180000000)
    {
        if (memoryCache.TryGetValue(CellsCacheKey, out PaginatedResponse<CellDto>? cachedCells))
            return Ok(cachedCells);

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
        var cells = await mediator.Send(request);
        if (cells.Succeeded && cells.Data != null)
            memoryCache.Set(CellsCacheKey, cells, TimeSpan.FromHours(1));

        return cells.Succeeded ? Ok(cells) : BadRequest(cells);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CellDto>>> GetCellById(Guid id)
    {
        var cell = await mediator.Send(new GetCellByIdRequest(id));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateCellById(Guid id, UpdateCellDto updateCell)
    {
        var cell = await mediator.Send(new UpdateCellByIdRequest(id, updateCell));
        if (!cell.Succeeded)
            return BadRequest(cell);

        memoryCache.Remove(CellsCacheKey);
        return Ok(cell);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateStatusById(Guid id, [FromBody] string status)
    {
        var cell = await mediator.Send(new UpdateCellStatusByIdRequest(id, status));
        if (!cell.Succeeded)
            return BadRequest(cell);

        memoryCache.Remove(CellsCacheKey);
        return Ok(cell);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellById([Required][FromQuery] List<Guid> ids)
    {
        var cells = await mediator.Send(new DeleteCellsByIdsRequest { Ids = ids });
        if (!cells.Succeeded)
            return BadRequest(cells);

        memoryCache.Remove(CellsCacheKey);
        return Ok(cells);
    }
}
