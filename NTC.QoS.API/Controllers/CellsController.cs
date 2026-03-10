using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Domain.Interfaces;
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
    public async Task<ActionResult<ApiResponse<CellDto>>> Create(
        [FromBody] CreateCellDto createCell, 
        [FromQuery] Guid? regionId = null,
        [FromServices] IUserContextService userContext = null!)
    {
        var cell = await mediator.Send(new CreateCellRequest(createCell) 
        { 
            RegionId = regionId ?? userContext?.GetCurrentRegionId()
        });
        if (!cell.Succeeded)
            return BadRequest(cell);

        if (cell.Data == null)
            return BadRequest(new ApiResponse<CellDto>
            {
                Succeeded = false,
                Message = "Cell creation failed, no data returned.",
            });

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
            "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "isVerified" })]
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
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "status", "serviceProviderIds", "providerIds", "serviceIds", "generationIds", "technologyIds", "bandIds", "longitude", "latitude", "distance", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "isVerified" })]
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

    /// <summary>
    /// Get all distinct technologies and their bands for a specific cell site.
    /// Results are grouped by technology and deduplicated — suitable for the Technologies tab on the cell site detail page.
    /// </summary>
    [HttpGet("{id}/technologies")]
    [RequirePermission(Permissions.ViewCells)]
    public async Task<ActionResult<ApiResponse<List<CellTechnologyBandDto>>>> GetCellTechnologies(Guid id)
    {
        var result = await mediator.Send(new GetCellTechnologiesRequest { CellId = id });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Cross-validate the cell site's active technologies and bands against
    /// regulatory frequency allocations. Returns per-combination compliance status
    /// with a warning flag for any unauthorized bands.
    /// </summary>
    [HttpGet("{id}/compliance")]
    [RequirePermission(Permissions.ViewCells)]
    public async Task<ActionResult<ApiResponse<CellComplianceDto>>> GetCellCompliance(Guid id)
    {
        var result = await mediator.Send(new GetCellComplianceRequest { CellId = id });
        return result.Succeeded ? Ok(result) : BadRequest(result);
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
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellById(
        [Required][FromQuery] List<Guid> ids,
        [FromQuery] Guid? regionId = null,
        [FromServices] IUserContextService userContext = null!)
    {
        var cells = await mediator.Send(new DeleteCellsByIdsRequest 
        { 
            Ids = ids,
            RegionId = regionId ?? userContext?.GetCurrentRegionId()
        });
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
             $"{string.Join(",", query.GenerationIds ?? [])}_" +
             $"{string.Join(",", query.TechnologyIds ?? [])}_" +
             $"{string.Join(",", query.BandIds ?? [])}_" +
               $"{query.Longitude}_{query.Latitude}_{query.Distance}_{query.IsVerified}";

        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<CellDto>? cachedCells))
            return Ok(cachedCells);

        var request = new GetAllCellsRequest(query.PageNumber, query.PageSize)
        {
            Search = query.Search,
            Status = query.Status,
            ServiceProviderIds = query.ServiceProviderIds,
            ProviderIds = query.ProviderIds,
            ServiceIds = query.ServiceIds,
            GenerationIds = query.GenerationIds,
            TechnologyIds = query.TechnologyIds,
            BandIds = query.BandIds,
            Coordinate = new(x: query.Longitude, y: query.Latitude),
            Distance = query.Distance,
            BarangayIds = query.BarangayIds,
            MunicipalityIds = query.MunicipalityIds,
            ProvinceIds = query.ProvinceIds,
            RegionIds = query.RegionIds,
            IsVerified = query.IsVerified
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

