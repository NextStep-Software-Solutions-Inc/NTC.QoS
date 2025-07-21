using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;

namespace NTC.QoS.API.Controllers;

[Authorize]
[ApiController]
[Route("cellular-coverages", Name = "cellular-coverages")]
public class CellularCoveragesController(IMediator mediator, IMemoryCache memoryCache) : ControllerBase
{
    private const string CellularCoverageCachePrefix = "cellularCoverage_";
    private const string CellularCoverageCacheKeysSet = "cellularCoverage_cache_keys";

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CellularCoverageDto>>> CreateCellularCoverage(CreateCellularCoverageDto cellularCoverage)
    {
        var request = await mediator.Send(new CreateCellularCoverageRequest
        {
            CellularCoverage = cellularCoverage
        });
        if (!request.Succeeded || request.Data == null)
            return BadRequest(request.Data == null ? "Cell Coverage creation failed, no data returned." : request);

        RemoveAllCellularCoverageCache();
        return CreatedAtAction(nameof(GetCellularCoverageById), new { id = request.Data.Id }, request);
    }

    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "networkIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellularCoverageDto>>> GetAllCellularCoverages(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageNumber = 1,
        [FromQuery] List<Guid>? networkIds = null,
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] double longitude = 0,
        [FromQuery] double latitude = 0,
        [FromQuery] double distance = 1500000000)
    {
        var cacheKey = $"{CellularCoverageCachePrefix}{pageSize}_{pageNumber}_{string.Join(",", networkIds ?? [])}" +
               $"{string.Join(",", barangayIds ?? new List<Guid>())}_" +
               $"{string.Join(",", municipalityIds ?? new List<Guid>())}_" +
               $"{string.Join(",", provinceIds ?? new List<Guid>())}_" +
               $"{string.Join(",", regionIds ?? new List<Guid>())}_" +
               $"_{longitude}_{latitude}_{distance}";

        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<CellularCoverageDto>? cachedCellularCoverage))
            return Ok(cachedCellularCoverage);

        var request = new GetAllCellularCoveragesRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            NetworkIds = networkIds,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance,
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds,
        };

        var cellularCoverages = await mediator.Send(request);
        if (cellularCoverages.Succeeded && cellularCoverages.Data != null)
        {
            memoryCache.Set(cacheKey, cellularCoverages, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }
        return cellularCoverages.Succeeded ? Ok(cellularCoverages) : BadRequest(cellularCoverages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CellularCoverageDto>>> GetCellularCoverageById(Guid id)
    {
        var cellularCoverage = await mediator.Send(new GetCellularCoverageByIdRequest(id));
        return cellularCoverage.Succeeded ? Ok(cellularCoverage) : NotFound(cellularCoverage);
    }

    [HttpDelete("networks/{networkId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellularCoverageByNetworkId(Guid networkId)
    {
        var result = await mediator.Send(new DeleteCellularCoverageByNetworkIdRequest
        {
            NetworkId = networkId
        });

        if (!result.Succeeded)
            return BadRequest(result);

        RemoveAllCellularCoverageCache();
        return Ok(result);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellularCoveragesByIds([Required][FromQuery] List<Guid> ids)
    {
        var cellularCoverages = await mediator.Send(new DeleteCellularCoveragesByIdsRequest { Ids = ids });
        if (!cellularCoverages.Succeeded)
            return BadRequest(cellularCoverages);

        RemoveAllCellularCoverageCache();
        return Ok(cellularCoverages);
    }

    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate<HashSet<string>>(CellularCoverageCacheKeysSet, _ => new HashSet<string>());
        if (keys != null)
        {
            keys.Add(key);
            memoryCache.Set(CellularCoverageCacheKeysSet, keys, TimeSpan.FromHours(1));
        }
    }

    private void RemoveAllCellularCoverageCache()
    {
        if (memoryCache.TryGetValue<HashSet<string>>(CellularCoverageCacheKeysSet, out var keys) && keys != null)
        {
            foreach (var key in keys)
                memoryCache.Remove(key);
            memoryCache.Remove(CellularCoverageCacheKeysSet);
        }
    }
}
