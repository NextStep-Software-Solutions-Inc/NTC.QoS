using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;

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

    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "serviceProviderIds", "cellIds", "generationIds", "providerIds", "serviceIds", "signalQuality", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllSpeedtests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? cellIds = null,
        [FromQuery] List<Guid>? generationIds = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<Guid>? serviceIds = null,
        string? signalQuality = null,
        double longitude = 0,
        double latitude = 0,
        double distance = 15000000)
    {
        var cacheKey = $"{SpeedtestsCachePrefix}{pageNumber}_{pageSize}_{search}_{string.Join(",", serviceProviderIds ?? [])}_" +
            $"{string.Join(",", barangayIds ?? new List<Guid>())}_" +
            $"{string.Join(",", municipalityIds ?? new List<Guid>())}_" +
            $"{string.Join(",", provinceIds ?? new List<Guid>())}_" +
            $"{string.Join(",", regionIds ?? new List<Guid>())}_" +
            $"_{string.Join(",", cellIds ?? [])}_{string.Join(",", generationIds ?? [])}_{string.Join(",", providerIds ?? [])}" + 
            $"_{string.Join(",", serviceIds ?? [])}_{signalQuality}_{longitude}_{latitude}_{distance}";
        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<SpeedtestDto>? cachedSpeedtests))
            return Ok(cachedSpeedtests);

        var request = new GetAllSpeedtestsRequest(pageNumber, pageSize)
        {
            Search = search,
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds,
            ServiceProviderIds = serviceProviderIds,
            CellIds = cellIds,
            GenerationIds = generationIds,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            SignalQuality = signalQuality,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance
        };
        var speedtests = await mediator.Send(request);
        if (speedtests.Succeeded && speedtests.Data != null)
        {
            memoryCache.Set(cacheKey, speedtests, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }
        return speedtests.Succeeded ? Ok(speedtests) : BadRequest(speedtests);
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
}
