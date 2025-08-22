using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("service-providers", Name = "service-providers")]
[Authorize]

public class ServiceProvidersController(IMediator mediator, ILogger<ServiceProvidersController> logger, IMemoryCache memoryCache) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<ServiceProvidersController> _logger = logger;
    private const string ServiceProviderCachePrefix = "serviceProvider_";
    private const string ServiceProviderCacheKeysSet = "serviceProvider_cache_keys";

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ServiceProviderDto>>> CreateServiceProvider(CreateServiceProviderDto createServiceProvider)
    {
        var serviceProvider = await _mediator.Send(new CreateServiceProviderRequest(createServiceProvider));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false,
    VaryByQueryKeys = new[] { "pageSize", "pageNumber", "providerIds", "serviceIds", "serviceTypes", "areas", "regions" })]
    public async Task<ActionResult<PaginatedResponse<ServiceProviderDto>>> GetAllServiceProviders(
    int pageSize = 10,
    int pageNumber = 1,
    [FromQuery] List<Guid>? providerIds = null,
    [FromQuery] List<Guid>? serviceIds = null,
    [FromQuery] List<string>? serviceTypes = null,
    [FromQuery] List<string>? areas = null,
    [FromQuery] List<string>? regions = null)
    {
        var cacheKey = $"{ServiceProviderCachePrefix}_{pageSize}_{pageNumber}_{string.Join(",", providerIds?.Select(p => p.ToString()) ?? [])}_{string.Join(",", serviceIds?.Select(s => s.ToString()) ?? [])}_{string.Join(",", serviceTypes ?? [])}_{string.Join(",", areas ?? [])}_{string.Join(",", regions ?? [])}";

        if (memoryCache.TryGetValue(cacheKey, out PaginatedResponse<ServiceProviderDto>? cached))
        {
            _logger.LogInformation("Cache HIT: {CacheKey}", cacheKey);
            return Ok(cached);
        }

        _logger.LogInformation("Cache MISS: {CacheKey}", cacheKey);

        var serviceProviders = await _mediator.Send(new GetAllServiceProvidersRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            ServiceTypes = serviceTypes,
            Areas = areas,
            Regions = regions
        });

        if (serviceProviders.Succeeded && serviceProviders.Data != null)
        {
            memoryCache.Set(cacheKey, serviceProviders, TimeSpan.FromMinutes(5));
            TrackCacheKey(cacheKey);
            _logger.LogInformation("Cache SET: {CacheKey}", cacheKey);
        }

        return serviceProviders.Succeeded ? Ok(serviceProviders) : BadRequest(serviceProviders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ServiceProviderDto>>> GetServiceProviderById(Guid id)
    {
        var serviceProvider = await _mediator.Send(new GetServiceProviderByIdRequest(id));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ServiceProviderDto>>> UpdateServiceProviderById(Guid id, UpdateServiceProviderDto updateServiceProvider)
    {
        var serviceProvider = await _mediator.Send(new UpdateServiceProviderByIdRequest(id, updateServiceProvider));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteServiceProviderById(Guid id)
    {
        var serviceProvider = await _mediator.Send(new DeleteServiceProviderByIdRequest(id));
        return serviceProvider.Succeeded ? Ok(serviceProvider) : BadRequest(serviceProvider);
    }

    [HttpGet("clear-cache")]
    public ActionResult<bool> PurgeServiceProviderCache()
    {
        RemoveAllServiceProviderCache();
        _logger.LogInformation("Service provider cache cleared");
        return Ok(true);
    }

    private void TrackCacheKey(string key)
    {
        var keys = memoryCache.GetOrCreate<HashSet<string>>(ServiceProviderCacheKeysSet, _ => new HashSet<string>());
        if (keys != null)
        {
            keys.Add(key);
            memoryCache.Set(ServiceProviderCacheKeysSet, keys, TimeSpan.FromHours(24));
        }
    }

    private void RemoveAllServiceProviderCache()
    {
        if (memoryCache.TryGetValue<HashSet<string>>(ServiceProviderCacheKeysSet, out var keys) && keys != null)
        {
            foreach (var key in keys)
            {
                memoryCache.Remove(key);
            }
            memoryCache.Remove(ServiceProviderCacheKeysSet);
            _logger.LogInformation("Removed {Count} cache entries", keys.Count);
        }
        else
        {
            _logger.LogInformation("No cache entries found to remove");
        }
    }
}
