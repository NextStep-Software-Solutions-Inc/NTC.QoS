using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("NAPs", Name = "NAPs")]
[Authorize]

public class NAPsController : ControllerBase
{
    private const int BrowserCacheDuration = 21600;
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memoryCache;
    private const string NAPsCachePrefix = "naps_";
    private const string NAPsCacheKeysSet = "naps_cache_keys";

    public NAPsController(IMediator mediator, IMemoryCache memoryCache)
    {
        _mediator = mediator;
        _memoryCache = memoryCache;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NAPDto>>> CreateNAP(CreateNAPDto createNAPDto)
    {
        var NAP = await _mediator.Send(new CreateNAPRequest(createNAPDto));
        if (NAP.Succeeded)
        {
            RemoveAllNAPsCache(); // Purge all NAPs cache
            return Ok(NAP);
        }
        return BadRequest(NAP);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllPublicNAPs([FromQuery] CellsQueryParams query)
    {
        return await GetAllNAPs(query);
    }

    [HttpGet]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllInternalNAPs([FromQuery] CellsQueryParams query)
    {
        return await GetAllNAPs(query);
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<ApiResponse<NAPDto>>> GetNAPById(Guid id)
    {
        var request = new GetNAPByIdRequest(id);
        var NAP = await _mediator.Send(request);
        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNAPsByIds([Required][FromQuery] List<Guid> ids)
    {
        var NAP = await _mediator.Send(new DeleteNAPsByIdsRequest { Ids = ids });
        if (NAP.Succeeded)
        {
            RemoveAllNAPsCache(); // Purge all NAPs cache
            return Ok(NAP);
        }
        return BadRequest(NAP);
    }

    private async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllNAPs(CellsQueryParams query)
    {
        var cacheKey = $"{NAPsCachePrefix}{query.PageSize}_{query.PageNumber}_" +
               $"{string.Join(",", query.ServiceProviderIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.BarangayIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.MunicipalityIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.ProvinceIds ?? new List<Guid>())}_" +
               $"{string.Join(",", query.RegionIds ?? new List<Guid>())}_" +
               $"_{query.Longitude}_{query.Latitude}_{query.Distance}";

        if (_memoryCache.TryGetValue(cacheKey, out PaginatedResponse<NAPDto>? cachedNAPs))
            return Ok(cachedNAPs);

        var NAP = await _mediator.Send(new GetAllNAPsRequest(pageSize: query.PageSize, pageNumber: query.PageNumber)
        {
            ServiceProviderIds = query.ServiceProviderIds,
            BarangayIds = query.BarangayIds,
            MunicipalityIds = query.MunicipalityIds,
            ProvinceIds = query.ProvinceIds,
            RegionIds = query.RegionIds,
            Coordinate = new(x: query.Longitude, y: query.Latitude),
            Distance = query.Distance
        });

        if (NAP.Succeeded && NAP.Data != null)
        {
            _memoryCache.Set(cacheKey, NAP, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }
    private void TrackCacheKey(string key)
    {
        var keys = _memoryCache.GetOrCreate<HashSet<string>>(NAPsCacheKeysSet, _ => new HashSet<string>());
        if (keys != null) // Ensure keys is not null before dereferencing  
        {
            keys.Add(key);
            _memoryCache.Set(NAPsCacheKeysSet, keys, TimeSpan.FromHours(1));
        }
    }

    private void RemoveAllNAPsCache()
    {
        if (_memoryCache.TryGetValue<HashSet<string>>(NAPsCacheKeysSet, out var keys) && keys != null)
        {
            foreach (var key in keys)
                _memoryCache.Remove(key);
            _memoryCache.Remove(NAPsCacheKeysSet);
        }
    }


    public sealed class CellsQueryParams
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; } = 1;
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 10;
        [JsonPropertyName("search")]
        public string? Search { get; set; }
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        [JsonPropertyName("serviceProviderIds")]
        public List<Guid>? ServiceProviderIds { get; set; }
        [JsonPropertyName("providerIds")]
        public List<Guid>? ProviderIds { get; set; }
        [JsonPropertyName("serviceIds")]
        public List<Guid>? ServiceIds { get; set; }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; } = 0;
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; } = 0;
        [JsonPropertyName("distance")]
        public double Distance { get; set; } = 15000000;
        [JsonPropertyName("barangayIds")]
        public List<Guid>? BarangayIds { get; set; }
        [JsonPropertyName("municipalityIds")]
        public List<Guid>? MunicipalityIds { get; set; }
        [JsonPropertyName("provinceIds")]
        public List<Guid>? ProvinceIds { get; set; }
        [JsonPropertyName("regionIds")]
        public List<Guid>? RegionIds { get; set; }
    }
}