using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Helper;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using System.ComponentModel.DataAnnotations;
using static NTC.QoS.Domain.Models.DTOs.Dashboard.DashboardAggregateDTO;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("dashboard", Name = "dashboard")]
public class DashboardController(IMediator mediator, ILogger<DashboardController> logger, IMemoryCache memoryCache) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<DashboardController> _logger = logger;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private const string DashboardCachePrefix = "dashboards_";
    private const string DashboardCacheKeysSet = "dashboard_cache_keys";

    [HttpGet("cell-counts")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalCellsitesCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;

        if (startDate.HasValue && endDate.HasValue)
        {
            // Use provided dates, convert to UTC
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            // Use backend-driven default from the helper
            (finalStartDate, finalEndDate) = DateTimeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (CellCounts): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate); ;
        }

        var request = new GetTotalCellsitesCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("NAPs-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalNAPsCount(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;

        if (startDate.HasValue && endDate.HasValue)
        {
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            (finalStartDate, finalEndDate) = DateTimeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (NAPsCount): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }

        var request = new GetTotalNAPsCountRequest { StartDate = finalStartDate, EndDate = finalEndDate };
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("regions/top-by-cell-count")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopRegionsByCellCount([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int limit = 0)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;

        if (startDate.HasValue && endDate.HasValue)
        {
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            (finalStartDate, finalEndDate) = DateTimeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (RegionsTopByCellCount): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }

        var query = new GetTopRegionsByCellCountRequest { Limit = limit, StartDate = finalStartDate, EndDate = finalEndDate };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("provinces/top-by-cell-count/region-id")]
    public async Task<ActionResult<ApiResponse<List<GeoEntityCellCountDto>>>> GetTopProvincesInRegionByCellCount([Required][FromQuery] Guid regionId, [FromQuery] int limit = 0)
    {
        var query = new GetTopProvincesInRegionByCellCountRequest { RegionId = regionId, Limit = limit };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetDashboardOverview(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? municipalityId = null,
        [FromQuery] int limit = 5)
    {
        DateTime finalStartDate;
        DateTime finalEndDate;
        if (startDate.HasValue && endDate.HasValue)
        {
            finalStartDate = startDate.Value.ToUniversalTime();
            finalEndDate = endDate.Value.ToUniversalTime();
        }
        else
        {
            (finalStartDate, finalEndDate) = DateTimeHelper.GetDefaultDateRange();
            _logger.LogInformation("Dashboard (Overview): No date range provided. Using default from helper: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", finalStartDate, finalEndDate);
        }
        var request = new GetDashboardOverviewRequest { StartDate = finalStartDate, EndDate = finalEndDate, RegionId = regionId, ProvinceId = provinceId, MunicipalityId = municipalityId, Limit = limit };
        var response = await _mediator.Send(request);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet]
    public async Task<ActionResult<DashboardAggregateDto>> GetCompleteDashboard(
    [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
    [FromQuery] Guid? regionId = null,
    [FromQuery] Guid? provinceId = null,
    [FromQuery] Guid? municipalityId = null,
    [FromQuery] int limit = 5)
    {
        var (finalStartDate, finalEndDate) = ResolveDateRange(startDate, endDate);
        string cacheKey = $"{DashboardCachePrefix}all_{finalStartDate:yyyyMMdd}_{finalEndDate:yyyyMMdd}_{regionId}_{provinceId}_{municipalityId}_{limit}";

        if (_memoryCache.TryGetValue(cacheKey, out DashboardAggregateDto? cached))
            return Ok(cached);

        var result = new DashboardAggregateDto
        {
            TotalCellsiteCount = await _mediator.Send(new GetTotalCellsitesCountRequest
            {
                StartDate = finalStartDate,
                EndDate = finalEndDate
            }),

            TotalNAPCount = await _mediator.Send(new GetTotalNAPsCountRequest
            {
                StartDate = finalStartDate,
                EndDate = finalEndDate
            }),

            TopRegions = await _mediator.Send(new GetTopRegionsByCellCountRequest
            {
                StartDate = finalStartDate,
                EndDate = finalEndDate,
                Limit = limit
            }),

            TopProvincesInRegion = await _mediator.Send(new GetTopProvincesInRegionByCellCountRequest
            {
                RegionId = regionId ?? Guid.Empty,
                Limit = limit
            }),

            Overview = await _mediator.Send(new GetDashboardOverviewRequest
            {
                StartDate = finalStartDate,
                EndDate = finalEndDate,
                RegionId = regionId,
                ProvinceId = provinceId,
                MunicipalityId = municipalityId,
                Limit = limit
            })
        };

        _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(24));
        TrackCacheKey(cacheKey);

        return Ok(result);
    }

    private void TrackCacheKey(string key)
    {
        var keys = _memoryCache.GetOrCreate<HashSet<string>>(DashboardCacheKeysSet, _ => new HashSet<string>());
        if (keys != null)
        {
            keys.Add(key);
            _memoryCache.Set(DashboardCacheKeysSet, keys, TimeSpan.FromHours(24));
        }
    }

    private void RemoveAllDashboardCache()
    {
        if (_memoryCache.TryGetValue<HashSet<string>>(DashboardCacheKeysSet, out var keys) && keys != null)
        {
            foreach (var key in keys)
                _memoryCache.Remove(key);
            _memoryCache.Remove(DashboardCacheKeysSet);
        }
    }

    private (DateTime finalStartDate, DateTime finalEndDate) ResolveDateRange(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return (startDate.Value.ToUniversalTime(), endDate.Value.ToUniversalTime());
        }

        return DateTimeHelper.GetDefaultDateRange();
    }
}
