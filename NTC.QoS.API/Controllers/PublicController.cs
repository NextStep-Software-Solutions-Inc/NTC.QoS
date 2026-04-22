using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.Cells.Requests;
using Microsoft.AspNetCore.Authorization;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private const int NAPMemoryCacheMinutes = 5;
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memoryCache;
    private const string PublicCacheKeysSet = "public_cache_keys";

    public PublicController(IMediator mediator, IMemoryCache memoryCache)
    {
        _mediator = mediator;
        _memoryCache = memoryCache;
    }

    [HttpGet("NAPs")]
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
        const string NAPCachePrefix = "public_nap_";
        var cacheKey = $"{NAPCachePrefix}{pageSize}_{pageNumber}_" +
               $"{string.Join(",", serviceProviderIds ?? [])}_" +
               $"{string.Join(",", barangayIds ?? [])}_" +
               $"{string.Join(",", municipalityIds ?? [])}_" +
               $"{string.Join(",", provinceIds ?? [])}_" +
               $"{string.Join(",", regionIds ?? [])}_" +
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

    [HttpGet("cells")]
    [ResponseCache(Duration = 1, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "status", "barangayIds", "municipalityIds", "provinceIds", "regionIds", "serviceProviderIds", "providerIds", "serviceIds", "generationIds", "technologyIds", "bandIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellDto>>> GetAllCells(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<Guid>? serviceIds = null,
        [FromQuery] List<Guid>? generationIds = null,
        [FromQuery] List<Guid>? technologyIds = null,
        [FromQuery] List<Guid>? bandIds = null,
        double longitude = 0,
        double latitude = 0,
        double distance = 180000000)
    {
        const string CellCachePrefix = "public_cell_";
        var cacheKey = $"{CellCachePrefix}{pageNumber}_{pageSize}_{search}_{status}_" +
               $"{string.Join(",", barangayIds ?? [])}_" +
               $"{string.Join(",", municipalityIds ?? [])}_" +
               $"{string.Join(",", provinceIds ?? [])}_" +
               $"{string.Join(",", regionIds ?? [])}_" +
               $"{string.Join(",", serviceProviderIds ?? [])}_" +
               $"{string.Join(",", providerIds ?? [])}_" +
               $"{string.Join(",", serviceIds ?? [])}_" +
               $"{string.Join(",", generationIds ?? [])}_" +
               $"{string.Join(",", technologyIds ?? [])}_" +
               $"{string.Join(",", bandIds ?? [])}_" +
               $"{longitude}_{latitude}_{distance}";

        if (_memoryCache.TryGetValue(cacheKey, out PaginatedResponse<CellDto>? cachedCells))
            return Ok(cachedCells);

        var request = new GetAllCellsRequest(pageNumber, pageSize)
        {
            Search = search,
            Status = status,
            ServiceProviderIds = serviceProviderIds,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            GenerationIds = generationIds,
            TechnologyIds = technologyIds,
            BandIds = bandIds,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance,
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds
        };
        var cells = await _mediator.Send(request);
        if (cells.Succeeded && cells.Data != null)
        {
            _memoryCache.Set(cacheKey, cells, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }

        return cells.Succeeded ? Ok(cells) : BadRequest(cells);
    }

    [HttpGet("speedtests")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "search", "serviceProviderIds", "cellIds", "generationIds", "providerIds", "serviceIds", "signalQuality", "longitude", "latitude", "distance", "isNoBroadbandCoverage" })]
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
        double distance = 15000000,
        [FromQuery] bool? isNoBroadbandCoverage = null)
    {
        const string SpeedtestCachePrefix = "public_speedtest_";
        var cacheKey = $"{SpeedtestCachePrefix}{pageNumber}_{pageSize}_{search}_{string.Join(",", serviceProviderIds ?? [])}_" +
            $"{string.Join(",", barangayIds ?? [])}_" +
            $"{string.Join(",", municipalityIds ?? [])}_" +
            $"{string.Join(",", provinceIds ?? [])}_" +
            $"{string.Join(",", regionIds ?? [])}_" +
            $"_{string.Join(",", cellIds ?? [])}_{string.Join(",", generationIds ?? [])}_{string.Join(",", providerIds ?? [])}" +
            $"_{string.Join(",", serviceIds ?? [])}_{signalQuality}_{longitude}_{latitude}_{distance}_{isNoBroadbandCoverage}";

        if (_memoryCache.TryGetValue(cacheKey, out PaginatedResponse<SpeedtestDto>? cachedSpeedtests))
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
            IsNoBroadbandCoverage = isNoBroadbandCoverage,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance
        };
        var speedtests = await _mediator.Send(request);
        if (speedtests.Succeeded && speedtests.Data != null)
        {
            _memoryCache.Set(cacheKey, speedtests, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }
        return speedtests.Succeeded ? Ok(speedtests) : BadRequest(speedtests);
    }

    [HttpGet("cellular-coverages")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "networkIds", "longitude", "latitude", "distance" })]
    public async Task<ActionResult<PaginatedResponse<CellularCoverageDto>>> GetAllCellularCoverages(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageNumber = 1,
        [FromQuery] List<Guid>? networkIds = null,
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] double longitude = 0,
        [FromQuery] double latitude = 0,
        [FromQuery] double distance = 1500000000)
    {
        const string CellularCoverageCachePrefix = "public_cellular_coverage_";
        var cacheKey = $"{CellularCoverageCachePrefix}{pageSize}_{pageNumber}_{string.Join(",", networkIds ?? [])}" +
               $"{string.Join(",", barangayIds ?? [])}_" +
               $"{string.Join(",", municipalityIds ?? [])}_" +
               $"{string.Join(",", provinceIds ?? [])}_" +
               $"{string.Join(",", regionIds ?? [])}_" +
               $"_{longitude}_{latitude}_{distance}";

        if (_memoryCache.TryGetValue(cacheKey, out PaginatedResponse<CellularCoverageDto>? cachedCellularCoverage))
            return Ok(cachedCellularCoverage);

        var request = new GetAllCellularCoveragesRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            NetworkIds = networkIds,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance,
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds,
        };

        var cellularCoverages = await _mediator.Send(request);
        if (cellularCoverages.Succeeded && cellularCoverages.Data != null)
        {
            _memoryCache.Set(cacheKey, cellularCoverages, TimeSpan.FromHours(1));
            TrackCacheKey(cacheKey);
        }
        return cellularCoverages.Succeeded ? Ok(cellularCoverages) : BadRequest(cellularCoverages);
    }

    /// <summary>
    /// Test endpoint for rate limiting functionality.
    /// Returns basic request information for testing purposes.
    /// </summary>
    [HttpGet("rate-limit-test")]
    public IActionResult TestRateLimit()
    {
        var response = new
        {
            message = "Rate limiting test endpoint",
            timestamp = DateTime.UtcNow,
            requestId = Guid.NewGuid().ToString(),
            server = Environment.MachineName
        };

        return Ok(response);
    }

    private void TrackCacheKey(string key)
    {
        var keys = _memoryCache.GetOrCreate<HashSet<string>>(PublicCacheKeysSet, _ => []);
        if (keys != null)
        {
            keys.Add(key);
            _memoryCache.Set(PublicCacheKeysSet, keys, TimeSpan.FromMinutes(NAPMemoryCacheMinutes));
        }
    }
}
