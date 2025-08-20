using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;

namespace NTC.QoS.API.Controllers;

[Authorize]
[ApiController]
[Route("speedtests", Name = "speedtests")]
public class SpeedtestsController(IMediator mediator, IMemoryCache memoryCache) : ControllerBase
{
    private const string SpeedtestsCachePrefix = "speedtest_";
    private const string SpeedtestsCacheKeysSet = "speedtest_cache_keys";

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> CreateSpeedtest(CreateSpeedtestDto createSpeedtest)
    {
        var speedtest = await mediator.Send(new CreateSpeedtestRequest(createSpeedtest));
        if (!speedtest.Succeeded || speedtest.Data == null)
            return BadRequest(speedtest.Data == null ? "Speedtest creation failed, no data returned." : speedtest);

        RemoveAllSpeedtestsCache();
        return CreatedAtAction(nameof(GetSpeedtestById), new { id = speedtest.Data.Id }, speedtest);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "serviceProviderIds", "cellIds", "generationIds", "providerIds", "serviceIds", "signalQuality", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllPublicSpeedtests([FromQuery] SpeedtestsQueryParams query)
    {
        return await GetAllSpeedtests(query);
    }

    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "serviceProviderIds", "cellIds", "generationIds", "providerIds", "serviceIds", "signalQuality", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllInternalSpeedtests([FromQuery] SpeedtestsQueryParams query)
    {
        return await GetAllSpeedtests(query);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> GetSpeedtestById(Guid id)
    {
        var speedtest = await mediator.Send(new GetSpeedtestByIdRequest(id));
        return speedtest.Succeeded ? Ok(speedtest) : BadRequest(speedtest);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSpeedtestById([Required][FromQuery] List<Guid> ids)
    {
        var speedtests = await mediator.Send(new DeleteSpeedtestsByIdsRequest { Ids = ids });
        if (!speedtests.Succeeded)
            return BadRequest(speedtests);

        RemoveAllSpeedtestsCache();
        return Ok(speedtests);
    }
    private async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllSpeedtests(SpeedtestsQueryParams query)
    {
        var cacheKey = $"{SpeedtestsCachePrefix}{query.PageNumber}_{query.PageSize}_{query.Search}_{string.Join(",", query.ServiceProviderIds ?? [])}_" +
            $"{string.Join(",", query.BarangayIds ?? new List<Guid>())}_" +
            $"{string.Join(",", query.MunicipalityIds ?? new List<Guid>())}_" +
            $"{string.Join(",", query.ProvinceIds ?? new List<Guid>())}_" +
            $"{string.Join(",", query.RegionIds ?? new List<Guid>())}_" +
            $"_{string.Join(",", query.CellIds ?? [])}_{string.Join(",", query.GenerationIds ?? [])}_{string.Join(",", query.ProviderIds ?? [])}" +
            $"_{string.Join(",", query.ServiceIds ?? [])}_{query.SignalQuality}_{query.Longitude}_{query.Latitude}_{query.Distance}";
        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<SpeedtestDto>? cachedSpeedtests))
            return Ok(cachedSpeedtests);

        var request = new GetAllSpeedtestsRequest(query.PageNumber, query.PageSize)
        {
            Search = query.Search,
            BarangayIds = query.BarangayIds,
            MunicipalityIds = query.MunicipalityIds,
            ProvinceIds = query.ProvinceIds,
            RegionIds = query.RegionIds,
            ServiceProviderIds = query.ServiceProviderIds,
            CellIds = query.CellIds,
            GenerationIds = query.GenerationIds,
            ProviderIds = query.ProviderIds,
            ServiceIds = query.ServiceIds,
            SignalQuality = query.SignalQuality,
            Coordinate = new(x: query.Longitude, y: query.Latitude),
            Distance = query.Distance
        };
        var speedtests = await mediator.Send(request);
        if (speedtests.Succeeded && speedtests.Data != null)
        {
            memoryCache.Set(cacheKey, speedtests, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }
        return speedtests.Succeeded ? Ok(speedtests) : BadRequest(speedtests);
    }
    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate<HashSet<string>>(SpeedtestsCacheKeysSet, _ => new HashSet<string>());
        if (keys != null)
        {
            keys.Add(key);
            memoryCache.Set(SpeedtestsCacheKeysSet, keys, TimeSpan.FromHours(1));
        }
    }

    private void RemoveAllSpeedtestsCache()
    {
        if (memoryCache.TryGetValue<HashSet<string>>(SpeedtestsCacheKeysSet, out var keys) && keys != null)
        {
            foreach (var key in keys)
                memoryCache.Remove(key);
            memoryCache.Remove(SpeedtestsCacheKeysSet);
        }
    }

    public sealed class SpeedtestsQueryParams
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 10;
        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("barangayIds")]
        public List<Guid>? BarangayIds { get; set; }

        [JsonPropertyName("municipalityIds")]
        public List<Guid>? MunicipalityIds { get; set; }

        [JsonPropertyName("provinceIds")]
        public List<Guid>? ProvinceIds { get; set; }

        [JsonPropertyName("regionIds")]
        public List<Guid>? RegionIds { get; set; }
        
        [JsonPropertyName("serviceProviderIds")]
        public List<Guid>? ServiceProviderIds { get; set; }

        [JsonPropertyName("providerIds")]
        public List<Guid>? ProviderIds { get; set; }

        [JsonPropertyName("serviceIds")]
        public List<Guid>? ServiceIds { get; set; }

        [JsonPropertyName("cellIds")]
        public List<Guid>? CellIds { get; set; }

        [JsonPropertyName("generationIds")]
        public List<Guid>? GenerationIds { get; set; }

        [JsonPropertyName("signalQuality")]
        public string? SignalQuality { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; } = 0;
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; } = 0;

        [JsonPropertyName("distance")]
        public double Distance { get; set; } = 15000000;
    }
}
