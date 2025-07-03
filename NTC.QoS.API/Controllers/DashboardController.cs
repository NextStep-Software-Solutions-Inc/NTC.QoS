using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("dashboard2", Name = "dashboard2")]
public class DashboardController(IMediator mediator, ILogger<DashboardController> logger, IMemoryCache memoryCache) : ControllerBase
{
    private readonly ILogger<DashboardController> _logger = logger;
    private const string DashboardCachePrefix = "dashboards_";
    private const string DashboardCacheKeysSet = "dashboard_cache_keys";

    /// <summary>
    /// Retrieves the total count of cellsites based on the specified date range.
    /// </summary>
    [HttpGet("cell-counts")]
    public async Task<ActionResult<ApiResponse<CellsiteCountsDto>>> GetTotalCellsitesCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);
        var request = new GetTotalCellsitesCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        return await SendMediatorRequest<GetTotalCellsitesCountRequest, CellsiteCountsDto>(request);
    }

    /// <summary>
    /// Retrieves the total count of NAPs (Network Access Points) based on the specified date range.
    /// </summary>
    [HttpGet("NAPs-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalNAPsCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);
        var request = new GetTotalNAPsCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        return await SendMediatorRequest<GetTotalNAPsCountRequest, int>(request);
    }

    /// <summary>
    /// Retrieves the top regions by cell count.
    /// </summary>
    [HttpGet("regions/top-by-cell-count")]
    public async Task<ActionResult<ApiResponse<List<TopRegionsByCellCountDto>>>> GetTopRegionsByCellCount(
       [FromQuery] DateTime? startDate,
       [FromQuery] DateTime? endDate,
       [FromQuery] int limit = 0)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger);
        var request = new GetTopRegionsByCellCountRequest2 { Limit = limit, StartDate = finalStartDate, EndDate = finalEndDate };
        return await SendMediatorRequest<GetTopRegionsByCellCountRequest2, List<TopRegionsByCellCountDto>>(request);
    }

    /// <summary>
    /// Retrieves the top provinces within a specific region by cell count.
    /// </summary>
    [HttpGet("provinces/top-by-cell-count/region-id")]
    public async Task<ActionResult<ApiResponse<List<TopProvincesInRegionByCellCountDto>>>> GetTopProvincesInRegionByCellCount(
       [FromQuery] DateTime? startDate,
       [FromQuery] DateTime? endDate,
       [FromQuery] Guid? regionId,
       [FromQuery] int limit = 0)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger);
        var request = new GetTopProvincesInRegionByCellCountRequest2 { StartDate = finalStartDate, EndDate = finalEndDate, RegionId = regionId, Limit = limit };
        return await SendMediatorRequest<GetTopProvincesInRegionByCellCountRequest2, List<TopProvincesInRegionByCellCountDto>>(request);
    }

    /// <summary>
    /// Retrieves a comprehensive overview for the dashboard, with caching.
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetDashboardOverview(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast,
        [FromQuery] Guid? regionId,
        [FromQuery] Guid? provinceId,
        [FromQuery] Guid? municipalityId,
        [FromQuery] int limit = 5)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);
        var cacheKey = $"{DashboardCachePrefix}overview_{finalStartDate:yyyyMMdd}_{finalEndDate:yyyyMMdd}_{regionId}_{provinceId}_{municipalityId}_{limit}";

        if (memoryCache.TryGetValue(cacheKey, out ApiResponse<DashboardOverviewDto>? cachedResponse) && cachedResponse != null)
        {
            _logger.LogInformation("Cache HIT: {CacheKey}", cacheKey);
            return Ok(cachedResponse);
        }

        _logger.LogInformation("Cache MISS: {CacheKey}", cacheKey);

        var request = new GetDashboardOverviewRequest
        {
            StartDate = finalStartDate,
            EndDate = finalEndDate,
            RegionId = regionId,
            ProvinceId = provinceId,
            MunicipalityId = municipalityId,
            Limit = limit
        };

        var response = await mediator.Send(request);
        if (response.Succeeded && response.Data != null)
        {
            SetCache(cacheKey, response, TimeSpan.FromHours(24));
        }

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Retrieves all aggregated dashboard data, with caching.
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<DashboardAggregateDTO>>> GetAllDashboard(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] int? daysInPast = null,
        [FromQuery] int limit = 5)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);
        string cacheKey = $"{DashboardCachePrefix}all_aggregate_{finalStartDate:yyyyMMdd}_{finalEndDate:yyyyMMdd}_{regionId}_{provinceId}_{municipalityId}_{limit}";

        if (memoryCache.TryGetValue(cacheKey, out ApiResponse<DashboardAggregateDTO>? cachedResponse) && cachedResponse != null)
        {
            _logger.LogInformation("Cache HIT: {CacheKey}", cacheKey);
            return Ok(cachedResponse);
        }

        _logger.LogInformation("Cache MISS: {CacheKey}", cacheKey);

        // Execute all MediatR requests in parallel
        var totalCellsitesCountTask = mediator.Send(new GetTotalCellsitesCountRequest { StartDate = finalStartDate, EndDate = finalEndDate });
        var totalNAPsCountTask = mediator.Send(new GetTotalNAPsCountRequest { StartDate = finalStartDate, EndDate = finalEndDate });
        var topRegionsTask = mediator.Send(new GetTopRegionsByCellCountRequest2 { StartDate = finalStartDate, EndDate = finalEndDate, Limit = limit });
        var topProvincesTask = mediator.Send(new GetTopProvincesInRegionByCellCountRequest2 { RegionId = regionId, StartDate = finalStartDate, EndDate = finalEndDate, Limit = limit });
        var topMunicipalitiesTask = mediator.Send(new GetTopMunicipalitiesInProvinceByCellCountRequest2 { ProvinceId = provinceId, StartDate = finalStartDate, EndDate = finalEndDate, Limit = limit });
        var topBarangaysTask = mediator.Send(new GetTopBarangaysInMunicipalityByCellCountRequest2 { MunicipalityId = municipalityId, StartDate = finalStartDate, EndDate = finalEndDate, Limit = limit });

        // Await all tasks concurrently
        await Task.WhenAll(
            totalCellsitesCountTask,
            totalNAPsCountTask,
            topRegionsTask,
            topProvincesTask,
            topMunicipalitiesTask,
            topBarangaysTask
        );

        // Assuming MediatR handlers return ApiResponse<T> and you want to aggregate the Data property
        var result = new DashboardAggregateDTO
        {
            TotalCellsitesCount = totalCellsitesCountTask.Result.Data,
            TotalNAPsCount = totalNAPsCountTask.Result.Data,
            TopRegionsByCellCount = topRegionsTask.Result.Data,
            TopProvincesInRegionByCellCount = topProvincesTask.Result.Data,
            TopMunicipalitiesInProvinceByCellCount = topMunicipalitiesTask.Result.Data,
            TopBarangaysInMunicipalityByCellCount = topBarangaysTask.Result.Data,
        };

        var apiResponse = new ApiResponse<DashboardAggregateDTO>(
            true,
            result,
            "Dashboard data retrieved successfully.",
            []
        );
        SetCache(cacheKey, apiResponse, TimeSpan.FromHours(24));

        return Ok(apiResponse);
    }

    /// <summary>
    /// Generic method to send a MediatR request and handle its API response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
    /// <typeparam name="TResponseData">The type of data expected in the ApiResponse.</typeparam>
    /// <param name="request">The MediatR request instance.</param>
    /// <returns>An ActionResult containing the ApiResponse.</returns>
    private async Task<ActionResult<ApiResponse<TResponseData>>> SendMediatorRequest<TRequest, TResponseData>(TRequest request)
        where TRequest : IRequest<ApiResponse<TResponseData>>
    {
        var response = await mediator.Send(request);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Sets an item in the memory cache and tracks its key.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The TimeSpan until the cache entry expires.</param>
    private void SetCache<T>(string key, T value, TimeSpan expiration)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expiration)
            .SetPriority(CacheItemPriority.Normal); // You can adjust priority as needed

        memoryCache.Set(key, value, cacheEntryOptions);
        TrackCacheKey(key);
        _logger.LogInformation("Cache SET: {CacheKey}", key);
    }

    /// <summary>
    /// Tracks a cache key in a dedicated set for invalidation purposes.
    /// </summary>
    /// <param name="key">The cache key to track.</param>
    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate(DashboardCacheKeysSet, entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(24)); // Expiration for the set of keys
            return new HashSet<string>();
        });

        if (keys != null) // Ensure keys is not null before locking
        {
            lock (keys) // Ensure thread-safe addition to the HashSet
            {
                keys.Add(key);
            }
            memoryCache.Set(DashboardCacheKeysSet, keys, TimeSpan.FromHours(24)); // Update the set with the new key
        }
    }

    /// <summary>
    /// Removes all dashboard-related cache entries.
    /// This method is designed for cache invalidation, e.g., after data updates.
    /// </summary>
    [HttpPost("clear-cache")] // Added an endpoint for cache clearing for demonstration/admin purposes
    public IActionResult ClearDashboardCache()
    {
        RemoveAllDashboardCache();
        _logger.LogInformation("All dashboard caches cleared.");

        return Ok(new ApiResponse<bool>(true, true, "All dashboard caches have been cleared."));
    }

    /// <summary>
    /// Removes all dashboard-related cache entries.
    /// This method is designed for cache invalidation, e.g., after data updates.
    /// </summary>
    private void RemoveAllDashboardCache()
    {
        if (memoryCache.TryGetValue<HashSet<string>>(DashboardCacheKeysSet, out var keys) && keys != null)
        {
            lock (keys) // Ensure thread-safe iteration and removal
            {
                foreach (var key in keys)
                {
                    memoryCache.Remove(key);
                    _logger.LogInformation("Cache REMOVED: {CacheKey}", key);
                }
                keys.Clear(); // Clear the set itself after removing all keys
            }
            memoryCache.Remove(DashboardCacheKeysSet); // Remove the set from cache as well
        }
    }
}