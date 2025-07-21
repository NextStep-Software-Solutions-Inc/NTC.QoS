using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cells", Name = "cells")]
[Authorize]


public class CellsController(IMediator mediator, IMemoryCache memoryCache) : ControllerBase
{
    private const string CellsCachePrefix = "cells_";
    private const string CellsCacheKeysSet = "cells_cache_keys";

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CellDto>>> CreateCell(CreateCellDto createCell)
    {
        var cell = await mediator.Send(new CreateCellRequest(createCell));
        if (!cell.Succeeded || cell.Data == null)
            return BadRequest(cell.Data == null ? "Cell creation failed, no data returned." : cell);

        RemoveAllCellsCache();
        return CreatedAtAction(nameof(GetCellById), new { id = cell.Data.Id }, cell);
    }

    [HttpGet]
    [ResponseCache(Duration = 1, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "status", "serviceProviderIds", "providerIds", "serviceIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllCells(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<Guid>? serviceIds = null,
        double longitude = 0,
        double latitude = 0,
        double distance = 180000000)
    {
        var cacheKey = $"{CellsCachePrefix}{pageNumber}_{pageSize}_{search}_{status}_" +
               $"{string.Join(",", barangayIds ?? new List<Guid>())}_" +
               $"{string.Join(",", municipalityIds ?? new List<Guid>())}_" +
               $"{string.Join(",", provinceIds ?? new List<Guid>())}_" +
               $"{string.Join(",", regionIds ?? new List<Guid>())}_" +
               $"{string.Join(",", serviceProviderIds ?? new List<Guid>())}_" +
               $"{string.Join(",", providerIds ?? new List<Guid>())}_" +
               $"{string.Join(",", serviceIds ?? new List<Guid>())}_" +
               $"{longitude}_{latitude}_{distance}";

        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<CellDto>? cachedCells))
            return Ok(cachedCells);

        var request = new GetAllCellsRequest(pageNumber, pageSize)
        {
            Search = search,
            Status = status,
            ServiceProviderIds = serviceProviderIds,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance,
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds
        };
        var cells = await mediator.Send(request);
        if (cells.Succeeded && cells.Data != null)
        {
            memoryCache.Set(cacheKey, cells, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }

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

        RemoveAllCellsCache();
        return Ok(cell);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateStatusById(Guid id, [FromBody] string status)
    {
        var cell = await mediator.Send(new UpdateCellStatusByIdRequest(id, status));
        if (!cell.Succeeded)
            return BadRequest(cell);

        RemoveAllCellsCache();
        return Ok(cell);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellById([Required][FromQuery] List<Guid> ids)
    {
        var cells = await mediator.Send(new DeleteCellsByIdsRequest { Ids = ids });
        if (!cells.Succeeded)
            return BadRequest(cells);

        RemoveAllCellsCache();
        return Ok(cells);
    }

    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate<HashSet<string>>(CellsCacheKeysSet, _ => new HashSet<string>());
        if (keys != null)
        {
            keys.Add(key);
            memoryCache.Set(CellsCacheKeysSet, keys, TimeSpan.FromHours(1));
        }
    }

    private void RemoveAllCellsCache()
    {
        if (memoryCache.TryGetValue<HashSet<string>>(CellsCacheKeysSet, out var keys) && keys != null)
        {
            foreach (var key in keys)
                memoryCache.Remove(key);
            memoryCache.Remove(CellsCacheKeysSet);
        }
    }
}
