using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("NAPs", Name = "NAPs")]
public class NAPsController : ControllerBase
{
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

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "longitude", "latitude", "distance" })]

    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllNAPs(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageNumber = 1,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] double longitude = 0,
        [FromQuery] double latitude = 0,
        [FromQuery] double distance = 15000000)
    {
        var cacheKey = $"{NAPsCachePrefix}{pageSize}_{pageNumber}_" +
               $"{string.Join(",", serviceProviderIds ?? new List<Guid>())}_" +
               $"{string.Join(",", barangayIds ?? new List<Guid>())}_" +
               $"{string.Join(",", municipalityIds ?? new List<Guid>())}_" +
               $"{string.Join(",", provinceIds ?? new List<Guid>())}_" +
               $"{string.Join(",", regionIds ?? new List<Guid>())}_" +
               $"_{longitude}_{latitude}_{distance}";

        if (_memoryCache.TryGetValue(cacheKey, out PaginatedResponse<NAPDto>? cachedNAPs))
            return Ok(cachedNAPs);

        var NAP = await _mediator.Send(new GetAllNAPsRequest(pageSize: pageSize, pageNumber: pageNumber)
        {
            ServiceProviderIds = serviceProviderIds,
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance
        });

        if (NAP.Succeeded && NAP.Data != null)
        {
            _memoryCache.Set(cacheKey, NAP, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpGet("{id}")]
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

}