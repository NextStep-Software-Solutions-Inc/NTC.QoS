using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("dashboard", Name = "dashboard")]
public class DashboardController(IMediator mediator, ILogger<DashboardController> logger, IMemoryCache memoryCache) : ControllerBase
{
    private readonly ILogger<DashboardController> _logger = logger;
    private const string DashboardCachePrefix = "dashboards_";
    private const string DashboardCacheKeysSet = "dashboard_cache_keys";

    [HttpGet("cell-counts")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalCellsitesCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        var request = new GetTotalCellsitesCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("NAPs-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalNAPsCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? daysInPast)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        var request = new GetTotalNAPsCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("regions/top-by-cell-count")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopRegionsByCellCount(
       [FromQuery] DateTime? startDate,
       [FromQuery] DateTime? endDate,
       [FromQuery] int limit = 0)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger);

        var query = new GetTopRegionsByCellCountRequest { Limit = limit, StartDate = finalStartDate, EndDate = finalEndDate };
        var result = await mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("provinces/top-by-cell-count/region-id")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopProvincesInRegionByCellCount(
       [FromQuery] DateTime? startDate,
       [FromQuery] DateTime? endDate,
       [FromQuery] Guid regionId,
       [FromQuery] int limit = 0)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger);

        var query = new GetTopProvincesInRegionByCellCountRequest { StartDate = finalStartDate, EndDate = finalEndDate, RegionId = regionId, Limit = limit };
        var result = await mediator.Send(query);
        return Ok(result);
    }

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

        var cacheKey = $"{DashboardCachePrefix}all_{finalStartDate:yyyyMMdd}_{finalEndDate:yyyyMMdd}_{regionId}_{provinceId}_{municipalityId}_{limit}";

        if (memoryCache.TryGetValue(cacheKey, out ApiResponse<DashboardAggregateDTO>? cached))
        {
            _logger.LogInformation("Cache HIT: {CacheKey}", cacheKey);
            return Ok(cached);
        }

        var totalCellCounts = new GetTotalCellsitesCountRequest();

        var request = new GetDashboardOverviewRequest { StartDate = finalStartDate, EndDate = finalEndDate, RegionId = regionId, ProvinceId = provinceId, MunicipalityId = municipalityId, Limit = limit };
        var response = await mediator.Send(request);
        if (response.Succeeded && response.Data != null)
        {
            memoryCache.Set(cacheKey, response, TimeSpan.FromHours(24));
            TrackCacheKey(cacheKey);
        }
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("all")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false,
    VaryByQueryKeys = new[] { "startDate", "endDate", "daysInPast", "regionId", "provinceId", "municipalityId", "limit" })]
    public async Task<ActionResult<DashboardAggregateDTO>> GetAllDashboard(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] int? daysInPast = null,
        [FromQuery] int limit = 5)
    {
        var (finalStartDate, finalEndDate) = DateTimeHelper.ResolveDateRange(startDate, endDate, _logger, daysInPast);

        string cacheKey = $"{DashboardCachePrefix}all_{finalStartDate:yyyyMMdd}_{finalEndDate:yyyyMMdd}_{regionId}_{provinceId}_{municipalityId}_{limit}";

        if (memoryCache.TryGetValue(cacheKey, out DashboardAggregateDTO? cached))
            return Ok(cached);

        var result = new DashboardAggregateDTO
        {
            TotalCellsitesCount = await mediator.Send(new GetTotalCellsitesCountRequest
            {
                StartDate = finalStartDate,
                EndDate = finalEndDate
            }),

            TotalNAPsCount = await mediator.Send(new GetTotalNAPsCountRequest
            {
                StartDate = finalStartDate,
                EndDate = finalEndDate
            }),

            TopRegionsByCellCount = await mediator.Send(new GetTopRegionsByCellCountRequest
            {
                StartDate = finalStartDate,
                EndDate = finalEndDate,
                Limit = limit
            }),

            TopProvincesInRegionByCellCount = await mediator.Send(new GetTopProvincesInRegionByCellCountRequest
            {
                RegionId = regionId,
                Limit = limit
            }),

            TopMunicipalitiesInProvinceByCellCount = await mediator.Send(new GetTopMunicipalitiesInProvinceByCellCountRequest
            {
                ProvinceId = provinceId,
                Limit = limit
            }),

            TopBarangaysInMunicipalityByCellCount = await mediator.Send(new GetTopBarangaysInMunicipalityByCellCountRequest
            {
                MunicipalityId = municipalityId,
                Limit = limit
            }),
        };

        memoryCache.Set(cacheKey, result, TimeSpan.FromHours(24));
        TrackCacheKey(cacheKey);

        return Ok(result);
    }

    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate<HashSet<string>>(DashboardCacheKeysSet, _ => new HashSet<string>());
        if (keys != null)
        {
            keys.Add(key);
            memoryCache.Set(DashboardCacheKeysSet, keys, TimeSpan.FromHours(24));
        }
    }
    private void RemoveAllDashboardCache()
    {
        if (memoryCache.TryGetValue<HashSet<string>>(DashboardCacheKeysSet, out var keys) && keys != null)
        {
            foreach (var key in keys)
                memoryCache.Remove(key);
            memoryCache.Remove(DashboardCacheKeysSet);
        }
    }
}
