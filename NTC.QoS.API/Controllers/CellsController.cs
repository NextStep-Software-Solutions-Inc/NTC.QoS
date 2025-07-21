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

    [HttpGet("public")]
    [AllowAnonymous]
    [ResponseCache(Duration = 1, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "status", "serviceProviderIds", "providerIds", "serviceIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllPublicCells([FromQuery] CellsQueryParams query)
    {
        return await GetAllCells(query);
    }
    [HttpGet]
    [ResponseCache(Duration = 1, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "status", "serviceProviderIds", "providerIds", "serviceIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetCellsInternal([FromQuery] CellsQueryParams query)
    {
        return await GetAllCells(query);
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
    
    private async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllCells(CellsQueryParams query)
    {
        var cacheKey = $"{CellsCachePrefix}{query.PageNumber}_{query.PageSize}_{query.Search}_{query.Status}_" +
               $"{string.Join(",", query.BarangayIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.MunicipalityIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.ProvinceIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.RegionIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.ServiceProviderIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.ProviderIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.ServiceIds ?? new List<Guid>())}_" +
               $"{query.Longitude}_{query.Latitude}_{query.Distance}";

        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<CellDto>? cachedCells))
            return Ok(cachedCells);

        var request = new GetAllCellsRequest(query.PageNumber, query.PageSize)
        {
            Search = query.Search,
            Status = query.Status,
            ServiceProviderIds = query.ServiceProviderIds,
            ProviderIds = query.ProviderIds,
            ServiceIds = query.ServiceIds,
            Coordinate = new(x: query.Longitude, y: query.Latitude),
            Distance = query.Distance,
            BarangayIds = query.BarangayIds,
            MunicipalityIds = query.MunicipalityIds,
            ProvinceIds = query.ProvinceIds,
            RegionIds = query.RegionIds
        };
        var cells = await mediator.Send(request);
        if (cells.Succeeded && cells.Data != null)
        {
            memoryCache.Set(cacheKey, cells, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }

        return cells.Succeeded ? Ok(cells) : BadRequest(cells);
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

    public sealed class CellsQueryParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public List<Guid>? BarangayIds { get; set; }
        public List<Guid>? MunicipalityIds { get; set; }
        public List<Guid>? ProvinceIds { get; set; }
        public List<Guid>? RegionIds { get; set; }
        public List<Guid>? ServiceProviderIds { get; set; }
        public List<Guid>? ProviderIds { get; set; }
        public List<Guid>? ServiceIds { get; set; }
        public double Longitude { get; set; } = 0;
        public double Latitude { get; set; } = 0;
        public double Distance { get; set; } = 180000000;
    }
    
}

