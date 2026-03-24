using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
using System.ComponentModel.DataAnnotations;
using NTC.QoS.Domain.Models.RequestParams;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("NAPs", Name = "NAPs")]
[Authorize]

public class NAPsController : ControllerBase
{
    private const int BrowserCacheDuration = 300;
    private const int NAPMemoryCacheMinutes = 5;
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memoryCache;
    private const string NAPsCachePrefix = "naps_";
    private const string NAPsCacheKeysSet = "naps_cache_keys";
    private const string PublicNAPsCachePrefix = "public_nap_";
    private const string PublicCacheKeysSet = "public_cache_keys";

    public NAPsController(IMediator mediator, IMemoryCache memoryCache)
    {
        _mediator = mediator;
        _memoryCache = memoryCache;
    }

    [HttpPost]
    [RequirePermission(Permissions.CreateNAPs)]
    public async Task<ActionResult<ApiResponse<NAPDto>>> CreateNAP(CreateNAPDto createNAPDto)
    {
        var NAP = await _mediator.Send(new CreateNAPRequest(createNAPDto));
        if (NAP.Succeeded)
        {
            RemoveAllNAPsCache();
            return Ok(NAP);
        }
        return BadRequest(NAP);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateNAPs)]
    public async Task<ActionResult<ApiResponse<NAPDto>>> PatchNAP(Guid id, UpdateNAPDto updateNAPDto)
    {
        var NAP = await _mediator.Send(new UpdateNAPRequest(id, updateNAPDto));
        if (NAP.Succeeded)
        {
            RemoveAllNAPsCache();
            return Ok(NAP);
        }
        return BadRequest(NAP);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllPublicNAPs([FromQuery] LocationQueryParams query)
    {
        return await GetAllNAPs(query);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewNAPs)]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllInternalNAPs([FromQuery] LocationQueryParams query)
    {
        return await GetAllNAPs(query, useCache: false);
    }

    [HttpDelete("cache")]
    [RequirePermission(Permissions.DeleteNAPs)]
    public ActionResult<ApiResponse<bool>> ClearNAPCaches()
    {
        RemoveAllNAPsCache();

        return Ok(new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "NAP caches cleared successfully."
        });
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewNAPs)]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<ApiResponse<NAPDto>>> GetNAPById(Guid id)
    {
        var request = new GetNAPByIdRequest(id);
        var NAP = await _mediator.Send(request);
        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpDelete]
    [RequirePermission(Permissions.DeleteNAPs)]
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

    private async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllNAPs(LocationQueryParams query, bool useCache = true)
    {
        var cacheKey = $"{NAPsCachePrefix}{query.PageSize}_{query.PageNumber}_" +
               $"{string.Join(",", query.ServiceProviderIds ?? [])}_" +
               $"{string.Join(",", query.BarangayIds ?? [])}_" +
               $"{string.Join(",", query.MunicipalityIds ?? [])}_" +
               $"{string.Join(",", query.ProvinceIds ?? [])}_" +
               $"{string.Join(",", query.RegionIds ?? [])}_" +
               $"_{query.Longitude}_{query.Latitude}_{query.Distance}";

        if (useCache && _memoryCache.TryGetValue(cacheKey, out PaginatedResponse<NAPDto>? cachedNAPs))
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

        if (useCache && NAP.Succeeded && NAP.Data != null)
        {
            _memoryCache.Set(cacheKey, NAP, TimeSpan.FromMinutes(NAPMemoryCacheMinutes));
            TrackCacheKey(cacheKey);
        }

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }
    private void TrackCacheKey(string key)
    {
        var keys = _memoryCache.GetOrCreate<HashSet<string>>(NAPsCacheKeysSet, _ => []);
        if (keys != null) // Ensure keys is not null before dereferencing  
        {
            keys.Add(key);
            _memoryCache.Set(NAPsCacheKeysSet, keys, TimeSpan.FromMinutes(NAPMemoryCacheMinutes));
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

        if (_memoryCache.TryGetValue<HashSet<string>>(PublicCacheKeysSet, out var publicKeys) && publicKeys != null)
        {
            foreach (var key in publicKeys.Where(k => k.StartsWith(PublicNAPsCachePrefix, StringComparison.Ordinal)))
            {
                _memoryCache.Remove(key);
            }

            publicKeys.RemoveWhere(k => k.StartsWith(PublicNAPsCachePrefix, StringComparison.Ordinal));
            if (publicKeys.Count == 0)
            {
                _memoryCache.Remove(PublicCacheKeysSet);
            }
            else
            {
                _memoryCache.Set(PublicCacheKeysSet, publicKeys, TimeSpan.FromMinutes(NAPMemoryCacheMinutes));
            }
        }
    }


    // ============================================================================
    // V2 VIEWPORT-BASED ENDPOINTS (Optimized for map rendering)
    // ============================================================================

    /// <summary>
    /// Get NAPs within a map viewport bounding box (v2 API - viewport-optimized).
    /// This endpoint is optimized for map UI rendering and prevents OOM issues.
    /// Uses spatial indexes and bounding box filtering instead of radius-based queries.
    /// Returns lightweight data without Guid IDs for optimal payload size.
    /// Route: GET /v2/NAPs/viewport
    /// </summary>
    /// <param name="query">Viewport query parameters including bounding box</param>
    /// <returns>Paginated list of NAPs within the viewport (without IDs)</returns>
    [HttpGet("/v2/NAPs/viewport")]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false,
        VaryByQueryKeys = new[] { "pageNumber", "pageSize", "minLat", "maxLat", "minLng", "maxLng",
            "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds" })]
    public async Task<ActionResult<PaginatedResponse<NAPViewportDto>>> GetAllNAPsViewport([FromQuery] ViewportQueryParams query)
    {
        // Validate viewport bounds
        if (!query.IsViewportValid())
        {
            return BadRequest(new ApiResponse<PaginatedResponse<NAPViewportDto>>
            {
                Succeeded = false,
                Message = "Invalid viewport bounds. Ensure minLatitude < maxLatitude and minLongitude < maxLongitude."
            });
        }

        // Generate cache key based on viewport
        var cacheKey = $"{NAPsCachePrefix}v2_viewport_{query.PageNumber}_{query.PageSize}_" +
                      $"{query.MinLat:F6}_{query.MaxLat:F6}_{query.MinLng:F6}_{query.MaxLng:F6}_" +
                      $"{string.Join(",", query.ServiceProviderIds ?? [])}_" +
                      $"{string.Join(",", query.BarangayIds ?? [])}_" +
                      $"{string.Join(",", query.MunicipalityIds ?? [])}_" +
                      $"{string.Join(",", query.ProvinceIds ?? [])}_" +
                      $"{string.Join(",", query.RegionIds ?? [])}";

        // Check cache
        if (_memoryCache.TryGetValue(cacheKey, out PaginatedResponse<NAPViewportDto>? cachedNAPs))
        {
            return Ok(cachedNAPs);
        }

        // Create viewport bounds DTO
        var viewport = query.ToViewportBounds();

        // Create and send request
        var request = new GetAllNAPsViewportRequest(query.PageNumber, query.PageSize, viewport)
        {
            Viewport = viewport,
            ServiceProviderIds = query.ServiceProviderIds,
            BarangayIds = query.BarangayIds,
            MunicipalityIds = query.MunicipalityIds,
            ProvinceIds = query.ProvinceIds,
            RegionIds = query.RegionIds
        };

        var naps = await _mediator.Send(request);

        // Cache successful results
        if (naps.Succeeded && naps.Data != null)
        {
            _memoryCache.Set(cacheKey, naps, TimeSpan.FromMinutes(NAPMemoryCacheMinutes));
            TrackCacheKey(cacheKey);
        }

        return naps.Succeeded ? Ok(naps) : BadRequest(naps);
    }

    /// <summary>
    /// Get NAPs within a map viewport bounding box - public endpoint (v2 API - viewport-optimized).
    /// Allows anonymous access for public map displays.
    /// Returns lightweight data without Guid IDs for optimal payload size.
    /// Route: GET /v2/NAPs/viewport/public
    /// </summary>
    /// <param name="query">Viewport query parameters including bounding box</param>
    /// <returns>Paginated list of NAPs within the viewport (without IDs)</returns>
    [HttpGet("/v2/NAPs/viewport/public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false,
        VaryByQueryKeys = new[] { "pageNumber", "pageSize", "minLat", "maxLat", "minLng", "maxLng",
            "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds" })]
    public async Task<ActionResult<PaginatedResponse<NAPViewportDto>>> GetAllPublicNAPsViewport([FromQuery] ViewportQueryParams query)
    {
        return await GetAllNAPsViewport(query);
    }
}