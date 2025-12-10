using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using NTC.QoS.Domain.Models.RequestParams;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;

namespace NTC.QoS.API.Controllers;

[Authorize]
[ApiController]
[Route("cellular-coverages", Name = "cellular-coverages")]
public class CellularCoveragesController(IMediator mediator, IMemoryCache memoryCache) : ControllerBase
{
    private const int BrowserCacheDuration = 21600;
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

    /// <summary>
    /// V2 API: Get cellular coverages within viewport (bounding box) - Public
    /// Returns minimal DTOs optimized for rendering millions of points on a map.
    /// </summary>
    [HttpGet("/v2/cellular-coverages/viewport/public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false,
        VaryByQueryKeys = new[] { "pageNumber", "pageSize", "minLat", "maxLat", "minLng", "maxLng",
            "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds" })]
    public async Task<ActionResult<PaginatedResponse<CellularCoverageViewportDto>>> GetAllPublicCellularCoveragesViewport([FromQuery] ViewportQueryParams query)
    {
        if (!query.IsViewportValid())
        {
            return BadRequest(new PaginatedResponse<CellularCoverageViewportDto>([], 1, query.PageSize, 0)
            {
                Succeeded = false,
                Message = "Invalid viewport bounds"
            });
        }

        var request = new GetAllCellularCoveragesViewportRequest(query);
        var result = await mediator.Send(request);
        return Ok(result);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "networkIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellularCoverageDto>>> GetAllPublicCellularCoverages([FromQuery] CellularCoverageQueryParams query)
    {
        return await GetAllCellularCoverages(query);
    }

    [HttpGet]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "networkIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellularCoverageDto>>> GetAllInternalCellularCoverages([FromQuery] CellularCoverageQueryParams query)
    {
        return await GetAllCellularCoverages(query);
    }

    [HttpGet("{id}")]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false)]
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

    /// <summary>
    /// V2: Get cellular coverage V2 records with pagination and filters
    /// </summary>
    [HttpGet("v2")]
    public async Task<ActionResult<PaginatedResponse<CellularCoverageV2Dto>>> GetCellularCoverageV2(
        [FromQuery] GetCellularCoverageV2Query query)
    {
        var result = await mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    private async Task<ActionResult<PaginatedResponse<CellularCoverageDto>>> GetAllCellularCoverages(CellularCoverageQueryParams query)
    {
        var cacheKey = $"{CellularCoverageCachePrefix}{query.PageSize}_{query.PageNumber}_{string.Join(",", query.NetworkIds ?? [])}" +
               $"_{query.CellId ?? Guid.Empty}_" +
               $"{string.Join(",", query.BarangayIds ?? [])}_" +
               $"{string.Join(",", query.MunicipalityIds ?? [])}_" +
               $"{string.Join(",", query.ProvinceIds ?? [])}_" +
               $"{string.Join(",", query.RegionIds ?? [])}_" +
               $"_{query.Longitude}_{query.Latitude}_{query.Distance}";

        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<CellularCoverageDto>? cachedCellularCoverage))
            return Ok(cachedCellularCoverage);

        var request = new GetAllCellularCoveragesRequest
        {
            PageSize = query.PageSize,
            PageNumber = query.PageNumber,
            NetworkIds = query.NetworkIds,
            CellId = query.CellId,
            Coordinate = new(x: query.Longitude, y: query.Latitude),
            Distance = query.Distance,
            BarangayIds = query.BarangayIds,
            MunicipalityIds = query.MunicipalityIds,
            ProvinceIds = query.ProvinceIds,
            RegionIds = query.RegionIds,
        };

        var cellularCoverages = await mediator.Send(request);
        if (cellularCoverages.Succeeded && cellularCoverages.Data != null)
        {
            memoryCache.Set(cacheKey, cellularCoverages, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }
        return cellularCoverages.Succeeded ? Ok(cellularCoverages) : BadRequest(cellularCoverages);
    }
    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate<HashSet<string>>(CellularCoverageCacheKeysSet, _ => []);
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

    public sealed class CellularCoverageQueryParams
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 10;

        [JsonPropertyName("networkIds")]
        public List<Guid>? NetworkIds { get; set; }

        [JsonPropertyName("cellId")]
        public Guid? CellId { get; set; }

        [JsonPropertyName("barangayIds")]
        public List<Guid>? BarangayIds { get; set; }

        [JsonPropertyName("municipalityIds")]
        public List<Guid>? MunicipalityIds { get; set; }

        [JsonPropertyName("provinceIds")]
        public List<Guid>? ProvinceIds { get; set; }

        [JsonPropertyName("regionIds")]
        public List<Guid>? RegionIds { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; } = 0;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; } = 0;

        [JsonPropertyName("distance")]
        public double Distance { get; set; } = 1500000000;
    }
}
