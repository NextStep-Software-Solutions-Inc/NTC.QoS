using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.RequestParams;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cells", Name = "cells")]
[Authorize]

public class CellsController(IMediator mediator, IMemoryCache memoryCache) : ControllerBase
{
    private const int BrowserCacheDuration = 21600;
    private const string CellsCachePrefix = "cells_";
    private const string CellsCacheKeysSet = "cells_cache_keys";

    [HttpPost]
    [RequirePermission(Permissions.CreateCells)]
    public async Task<ActionResult<ApiResponse<CellDto>>> CreateCell(CreateCellDto createCell)
    {
        var cell = await mediator.Send(new CreateCellRequest(createCell));
        if (!cell.Succeeded || cell.Data == null)
            return BadRequest(cell.Data == null ? "Cell creation failed, no data returned." : cell);

        RemoveAllCellsCache();
        return CreatedAtAction(nameof(GetCellById), new { id = cell.Data.Id }, cell);
    }

    /// <summary>
    /// v2 Viewport API: Get cells within viewport bounds (OPTIMIZED FOR MILLIONS OF DATA POINTS)
    /// </summary>
    [HttpGet("/v2/cells/viewport/public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false,
        VaryByQueryKeys = new[] { "pageNumber", "pageSize", "minLat", "maxLat", "minLng", "maxLng",
            "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds" })]
    public async Task<ActionResult<PaginatedResponse<CellViewportDto>>> GetAllPublicCellsViewport([FromQuery] ViewportQueryParams query)
    {
        if (!query.IsViewportValid())
        {
            return BadRequest(new PaginatedResponse<CellViewportDto>([], 1, query.PageSize, 0)
            {
                Succeeded = false,
                Message = "Invalid viewport bounds"
            });
        }

        var request = new GetAllCellsViewportRequest(query);
        var result = await mediator.Send(request);
        return Ok(result);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "status", "serviceProviderIds", "providerIds", "serviceIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllPublicCells([FromQuery] LocationQueryParams query)
    {
        return await GetAllCells(query);
    }
    [HttpGet]
    [RequirePermission(Permissions.ViewCells)]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "status", "serviceProviderIds", "providerIds", "serviceIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetCellsInternal([FromQuery] LocationQueryParams query)
    {
        return await GetAllCells(query);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewCells)]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<ApiResponse<CellDto>>> GetCellById(Guid id)
    {
        var cell = await mediator.Send(new GetCellByIdRequest(id));
        return cell.Succeeded ? Ok(cell) : BadRequest(cell);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateCells)]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateCellById(Guid id, UpdateCellDto updateCell)
    {
        var cell = await mediator.Send(new UpdateCellByIdRequest(id, updateCell));
        if (!cell.Succeeded)
            return BadRequest(cell);

        RemoveAllCellsCache();
        return Ok(cell);
    }

    [HttpPut("{id}/status")]
    [RequirePermission(Permissions.UpdateCells)]
    public async Task<ActionResult<ApiResponse<CellDto>>> UpdateStatusById(Guid id, [FromBody] string status)
    {
        var cell = await mediator.Send(new UpdateCellStatusByIdRequest(id, status));
        if (!cell.Succeeded)
            return BadRequest(cell);

        RemoveAllCellsCache();
        return Ok(cell);
    }

    [HttpDelete]
    [RequirePermission(Permissions.DeleteCells)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellById([Required][FromQuery] List<Guid> ids)
    {
        var cells = await mediator.Send(new DeleteCellsByIdsRequest { Ids = ids });
        if (!cells.Succeeded)
            return BadRequest(cells);

        RemoveAllCellsCache();
        return Ok(cells);
    }

    private async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllCells(LocationQueryParams query)
    {
        var cacheKey = $"{CellsCachePrefix}{query.PageNumber}_{query.PageSize}_{query.Search}_{query.Status}_" +
               $"{string.Join(",", query.BarangayIds ?? [])}_" +
               $"{string.Join(",", query.MunicipalityIds ?? [])}_" +
               $"{string.Join(",", query.ProvinceIds ?? [])}_" +
               $"{string.Join(",", query.RegionIds ?? [])}_" +
               $"{string.Join(",", query.ServiceProviderIds ?? [])}_" +
               $"{string.Join(",", query.ProviderIds ?? [])}_" +
               $"{string.Join(",", query.ServiceIds ?? [])}_" +
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
        var keys = memoryCache.GetOrCreate<HashSet<string>>(CellsCacheKeysSet, _ => []);
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

