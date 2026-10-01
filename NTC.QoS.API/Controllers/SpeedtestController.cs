using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.RequestParams;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NTC.QoS.API.Controllers;

[Authorize]
[ApiController]
[Route("speedtests", Name = "speedtests")]
public class SpeedtestsController(IMediator mediator, IMemoryCache memoryCache) : ControllerBase
{
    private const int BrowserCacheDuration = 21600;
    private const string SpeedtestsCachePrefix = "speedtest_";
    private const string SpeedtestsCacheKeysSet = "speedtest_cache_keys";

    // The list cache key is derived from SpeedtestsCacheVersionDto (row count +
    // last-modified timestamp), not just query params, so it changes automatically
    // whenever the Speedtests table changes — through this API, a job, or raw SQL.
    // That means staleness is now bounded by how often we re-check the version, not
    // by this TTL, so this can safely be generous.
    private static readonly TimeSpan SpeedtestsCacheDuration = TimeSpan.FromMinutes(5);

    // How long a fetched version signal is trusted before re-querying the DB. This is
    // the real staleness bound: worst case, a write is invisible for this long. Kept
    // short and cheap (one grouped aggregate query) rather than running it per request.
    private static readonly TimeSpan SpeedtestsVersionCacheDuration = TimeSpan.FromSeconds(5);
    private const string SpeedtestsVersionCacheKey = "speedtest_version";

    [HttpPost]
    [RequirePermission(Permissions.CreateSpeedtests)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateSpeedtest(CreateSpeedtestDto createSpeedtest)
    {
        var speedtest = await mediator.Send(new CreateSpeedtestRequest(createSpeedtest));
        if (!speedtest.Succeeded) return BadRequest(speedtest);

        RemoveAllSpeedtestsCache();
        return CreatedAtAction(nameof(GetSpeedtestById), new { id = speedtest.Data }, speedtest);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateSpeedtests)]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> UpdateSpeedtest(Guid id, UpdateSpeedtestDto updateSpeedtest)
    {
        var speedtest = await mediator.Send(new UpdateSpeedtestRequest(id, updateSpeedtest));
        if (!speedtest.Succeeded) return BadRequest(speedtest);

        RemoveAllSpeedtestsCache();
        return Ok(speedtest);
    }

    /// <summary>
    /// v2 Viewport API: Get speedtests within viewport bounds (OPTIMIZED FOR MILLIONS OF DATA POINTS)
    /// Uses spatial bounding box for efficient PostGIS queries.
    /// Returns minimal SpeedtestViewportDto (7 fields, no Guid ID) for map rendering.
    /// </summary>
    [HttpGet("/v2/speedtests/viewport/public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false,
        VaryByQueryKeys = new[] { "pageNumber", "pageSize", "minLat", "maxLat", "minLng", "maxLng",
            "serviceProviderIds", "barangayIds", "municipalityIds", "provinceIds", "regionIds",
            "minDownloadSpeed", "maxDownloadSpeed", "minUploadSpeed", "maxUploadSpeed", "startDate", "endDate",
            "isNoBroadbandCoverage" })]
    public async Task<ActionResult<PaginatedResponse<SpeedtestViewportDto>>> GetAllPublicSpeedtestsViewport([FromQuery] SpeedtestViewportQueryParams query)
    {
        var request = new GetAllSpeedtestsViewportRequest(query);
        var result = await mediator.Send(request);
        return Ok(result);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "serviceProviderIds", "cellIds", "generationIds", "providerIds", "serviceIds", "signalQuality", "minDownloadSpeed", "maxDownloadSpeed", "minUploadSpeed", "maxUploadSpeed", "startDate", "endDate", "longitude", "latitude", "distance", "isNoBroadbandCoverage" })]
    public async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllPublicSpeedtests([FromQuery] SpeedtestsQueryParams query)
    {
        return await GetAllSpeedtests(query);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewSpeedtests)]
    // [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "serviceProviderIds", "cellIds", "generationIds", "providerIds", "serviceIds", "signalQuality", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllInternalSpeedtests([FromQuery] SpeedtestsQueryParams query)
    {
        return await GetAllSpeedtests(query);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewSpeedtests)]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> GetSpeedtestById(Guid id)
    {
        var speedtest = await mediator.Send(new GetSpeedtestByIdRequest(id));
        return speedtest.Succeeded ? Ok(speedtest) : BadRequest(speedtest);
    }

    [HttpDelete]
    [RequirePermission(Permissions.DeleteSpeedtests)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSpeedtestById([Required][FromQuery] List<Guid> ids)
    {
        var speedtests = await mediator.Send(new DeleteSpeedtestsByIdsRequest { Ids = ids });
        if (!speedtests.Succeeded)
            return BadRequest(speedtests);

        RemoveAllSpeedtestsCache();
        return Ok(speedtests);
    }

    /// <summary>
    /// Manually busts the in-memory speedtests cache on this instance — both the cached
    /// version signal and all cached list results. Useful right after an out-of-band
    /// write (manual DB edits, scripts, migrations) if you don't want to wait out
    /// SpeedtestsVersionCacheDuration for the version to be re-checked.
    /// NOTE: IMemoryCache is per-process. If the API runs as multiple replicas, this
    /// only clears the instance that receives the request — call it on every replica,
    /// or move to a shared cache (e.g. Redis) once horizontal scaling is in play.
    /// </summary>
    [HttpPost("cache/clear")]
    [RequirePermission(Permissions.DeleteSpeedtests)] // consider a dedicated CacheManagement permission
    public ActionResult<ApiResponse<bool>> ClearSpeedtestsCache()
    {
        RemoveAllSpeedtestsCache();
        return Ok(new ApiResponse<bool>
        {
            Succeeded = true,
            Message = "Speedtest cache cleared.",
            Data = true
        });
    }

    private async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllSpeedtests(SpeedtestsQueryParams query)
    {
        var version = await GetSpeedtestsVersionAsync();

        var normalizedStartDate = query.StartDate.NormalizeFilterStartDate();
        var normalizedEndDate = query.EndDate.NormalizeFilterEndDate();

        var cacheKey = $"{SpeedtestsCachePrefix}v{version.Count}_{version.LastModifiedAt:O}_{query.PageNumber}_{query.PageSize}_{query.Search}_{string.Join(",", query.ServiceProviderIds ?? [])}_" +
            $"{string.Join(",", query.BarangayIds ?? [])}_" +
            $"{string.Join(",", query.MunicipalityIds ?? [])}_" +
            $"{string.Join(",", query.ProvinceIds ?? [])}_" +
            $"{string.Join(",", query.RegionIds ?? [])}_" +
            $"_{string.Join(",", query.CellIds ?? [])}_{string.Join(",", query.GenerationIds ?? [])}_{string.Join(",", query.ProviderIds ?? [])}" +
            $"_{string.Join(",", query.ServiceIds ?? [])}_{query.SignalQuality}_{query.MinDownloadSpeed}_{query.MaxDownloadSpeed}" +
            $"_{query.MinUploadSpeed}_{query.MaxUploadSpeed}_{normalizedStartDate:O}_{normalizedEndDate:O}_{query.Longitude}_{query.Latitude}_{query.Distance}_{query.IsNoBroadbandCoverage}";

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
            MinDownloadSpeed = query.MinDownloadSpeed,
            MaxDownloadSpeed = query.MaxDownloadSpeed,
            MinUploadSpeed = query.MinUploadSpeed,
            MaxUploadSpeed = query.MaxUploadSpeed,
            StartDate = normalizedStartDate,
            EndDate = normalizedEndDate,
            IsNoBroadbandCoverage = query.IsNoBroadbandCoverage,
            Coordinate = new(x: query.Longitude, y: query.Latitude),
            Distance = query.Distance
        };
        var speedtests = await mediator.Send(request);
        if (speedtests.Succeeded && speedtests.Data != null)
        {
            memoryCache.Set(cacheKey, speedtests, SpeedtestsCacheDuration);
            TrackCacheKey(cacheKey);
        }
        return speedtests.Succeeded ? Ok(speedtests) : BadRequest(speedtests);
    }

    /// <summary>
    /// Returns the current speedtests version signal, using a short-lived in-memory
    /// cache so the underlying aggregate query runs at most once per
    /// SpeedtestsVersionCacheDuration rather than once per request.
    /// </summary>
    private async Task<SpeedtestsCacheVersionDto> GetSpeedtestsVersionAsync()
    {
        if (memoryCache.TryGetValue(SpeedtestsVersionCacheKey, out SpeedtestsCacheVersionDto? cachedVersion) && cachedVersion != null)
            return cachedVersion;

        var result = await mediator.Send(new GetSpeedtestsCacheVersionRequest());
        var version = result.Succeeded && result.Data != null
            ? result.Data
            : new SpeedtestsCacheVersionDto(0, DateTime.MinValue);

        memoryCache.Set(SpeedtestsVersionCacheKey, version, SpeedtestsVersionCacheDuration);
        return version;
    }

    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate<HashSet<string>>(SpeedtestsCacheKeysSet, _ => []);
        if (keys != null)
        {
            keys.Add(key);
            memoryCache.Set(SpeedtestsCacheKeysSet, keys, SpeedtestsCacheDuration);
        }
    }

    private void RemoveAllSpeedtestsCache()
    {
        memoryCache.Remove(SpeedtestsVersionCacheKey);

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

        [JsonPropertyName("minDownloadSpeed")]
        public decimal? MinDownloadSpeed { get; set; }

        [JsonPropertyName("maxDownloadSpeed")]
        public decimal? MaxDownloadSpeed { get; set; }

        [JsonPropertyName("minUploadSpeed")]
        public decimal? MinUploadSpeed { get; set; }

        [JsonPropertyName("maxUploadSpeed")]
        public decimal? MaxUploadSpeed { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("isNoBroadbandCoverage")]
        public bool? IsNoBroadbandCoverage { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; } = 0;
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; } = 0;

        [JsonPropertyName("distance")]
        public double Distance { get; set; } = 15000000;
    }
}
