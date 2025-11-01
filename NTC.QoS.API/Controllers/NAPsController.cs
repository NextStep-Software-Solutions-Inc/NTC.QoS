using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.RequestParams;

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
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllInternalNAPs([FromQuery] CellsQueryParams query)
    {
        return await GetAllNAPs(query);
    }

    [HttpGet("{id}")]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false)]
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
               $"{string.Join(",", query.ServiceProviderIds ?? [])}_" +
               $"{string.Join(",", query.BarangayIds ?? [])}_" +
               $"{string.Join(",", query.MunicipalityIds ?? [])}_" +
               $"{string.Join(",", query.ProvinceIds ?? [])}_" +
               $"{string.Join(",", query.RegionIds ?? [])}_" +
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
        var keys = _memoryCache.GetOrCreate<HashSet<string>>(NAPsCacheKeysSet, _ => []);
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
        VaryByQueryKeys = new[] { "pageNumber", "pageSize", "minLatitude", "maxLatitude", "minLongitude", "maxLongitude", 
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
                      $"{query.MinLatitude:F6}_{query.MaxLatitude:F6}_{query.MinLongitude:F6}_{query.MaxLongitude:F6}_" +
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
        var viewport = new ViewportBoundsDto
        {
            MinLatitude = query.MinLatitude,
            MaxLatitude = query.MaxLatitude,
            MinLongitude = query.MinLongitude,
            MaxLongitude = query.MaxLongitude
        };

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
            _memoryCache.Set(cacheKey, naps, TimeSpan.FromHours(1));
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
        VaryByQueryKeys = new[] { "pageNumber", "pageSize", "minLatitude", "maxLatitude", "minLongitude", "maxLongitude",
            "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds" })]
    public async Task<ActionResult<PaginatedResponse<NAPViewportDto>>> GetAllPublicNAPsViewport([FromQuery] ViewportQueryParams query)
    {
        return await GetAllNAPsViewport(query);
    }

    // ============================================================================
    // QUERY PARAMETER CLASSES
    // ============================================================================

    /// <summary>
    /// Query parameters for v2 viewport-based NAP queries.
    /// Uses rectangular bounding box instead of radius-based distance filtering.
    /// Enforces resource limits for environments handling millions of data points.
    /// </summary>
    public sealed class ViewportQueryParams
    {
        [JsonPropertyName("pageNumber")]
        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1")]
        public int PageNumber { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        [Range(1, GeoSpatialConstants.MaxViewportPageSize, 
            ErrorMessage = "PageSize must be between 1 and " + nameof(GeoSpatialConstants.MaxViewportPageSize))]
        public int PageSize { get; set; } = GeoSpatialConstants.RecommendedViewportPageSize;

        [JsonPropertyName("minLatitude")]
        [Range(-90, 90)]
        public double MinLatitude { get; set; }

        [JsonPropertyName("maxLatitude")]
        [Range(-90, 90)]
        public double MaxLatitude { get; set; }

        [JsonPropertyName("minLongitude")]
        [Range(-180, 180)]
        public double MinLongitude { get; set; }

        [JsonPropertyName("maxLongitude")]
        [Range(-180, 180)]
        public double MaxLongitude { get; set; }

        [JsonPropertyName("serviceProviderIds")]
        public List<Guid>? ServiceProviderIds { get; set; }

        [JsonPropertyName("barangayIds")]
        public List<Guid>? BarangayIds { get; set; }

        [JsonPropertyName("municipalityIds")]
        public List<Guid>? MunicipalityIds { get; set; }

        [JsonPropertyName("provinceIds")]
        public List<Guid>? ProvinceIds { get; set; }

        [JsonPropertyName("regionIds")]
        public List<Guid>? RegionIds { get; set; }

        /// <summary>
        /// Validates that viewport bounds are logically correct
        /// </summary>
        public bool IsViewportValid()
        {
            return MinLatitude < MaxLatitude && MinLongitude < MaxLongitude;
        }
    }

    /// <summary>
    /// Query parameters for v1 distance-based NAP queries (legacy - maintained for backward compatibility).
    /// </summary>
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