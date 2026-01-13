using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Service for caching and retrieving rubric configurations
/// </summary>
public class RubricCacheService : IRubricCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IRubricConfigurationRepository _rubricRepository;
    private readonly ILogger<RubricCacheService> _logger;

    // Cache keys
    private const string ActiveRubricCacheKey = "rubric:active";
    private const string RubricByVersionCacheKeyPrefix = "rubric:version:";
    private const string RubricByIdCacheKeyPrefix = "rubric:id:";

    // Cache expiration - rubrics are relatively stable, cache for longer
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(4);

    public RubricCacheService(
        IMemoryCache memoryCache,
        IRubricConfigurationRepository rubricRepository,
        ILogger<RubricCacheService> logger)
    {
        _memoryCache = memoryCache;
        _rubricRepository = rubricRepository;
        _logger = logger;
    }

    public async Task<RubricConfiguration?> GetActiveRubricAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_memoryCache.TryGetValue<RubricConfiguration>(ActiveRubricCacheKey, out var cachedRubric))
        {
            _logger.LogDebug("Active rubric found in cache");
            return cachedRubric;
        }

        // Load from database
        _logger.LogDebug("Active rubric not in cache, loading from database");
        var rubric = await _rubricRepository.GetActiveRubricAsync(cancellationToken);

        if (rubric != null)
        {
            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.High,
                Size = 10 // Estimated 10KB for rubric with all relations
            };

            _memoryCache.Set(ActiveRubricCacheKey, rubric, cacheOptions);
            _logger.LogInformation("Cached active rubric: {Version}", rubric.Version);
        }
        else
        {
            _logger.LogWarning("No active rubric found in database");
        }

        return rubric;
    }

    public async Task<RubricConfiguration?> GetRubricByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{RubricByVersionCacheKeyPrefix}{version}";

        // Try to get from cache first
        if (_memoryCache.TryGetValue<RubricConfiguration>(cacheKey, out var cachedRubric))
        {
            _logger.LogDebug("Rubric version {Version} found in cache", version);
            return cachedRubric;
        }

        // Load from database
        _logger.LogDebug("Rubric version {Version} not in cache, loading from database", version);
        var rubric = await _rubricRepository.GetByVersionAsync(version, cancellationToken);

        if (rubric != null)
        {
            // Get full rubric with related entities
            rubric = await _rubricRepository.GetByIdWithRelatedAsync(rubric.Id, cancellationToken);

            if (rubric != null)
            {
                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiration,
                    Priority = CacheItemPriority.Normal,
                    Size = 10 // Estimated 10KB
                };

                _memoryCache.Set(cacheKey, rubric, cacheOptions);
                _logger.LogInformation("Cached rubric version: {Version}", version);
            }
        }
        else
        {
            _logger.LogWarning("Rubric version {Version} not found in database", version);
        }

        return rubric;
    }

    public async Task<RubricConfiguration?> GetRubricByIdAsync(Guid rubricId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{RubricByIdCacheKeyPrefix}{rubricId}";

        // Try to get from cache first
        if (_memoryCache.TryGetValue<RubricConfiguration>(cacheKey, out var cachedRubric))
        {
            _logger.LogDebug("Rubric {RubricId} found in cache", rubricId);
            return cachedRubric;
        }

        // Load from database
        _logger.LogDebug("Rubric {RubricId} not in cache, loading from database", rubricId);
        var rubric = await _rubricRepository.GetByIdWithRelatedAsync(rubricId, cancellationToken);

        if (rubric != null)
        {
            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.Normal,
                Size = 10 // Estimated 10KB
            };

            _memoryCache.Set(cacheKey, rubric, cacheOptions);
            _logger.LogInformation("Cached rubric: {Version} (ID: {RubricId})", rubric.Version, rubricId);
        }
        else
        {
            _logger.LogWarning("Rubric {RubricId} not found in database", rubricId);
        }

        return rubric;
    }

    public Task InvalidateCacheAsync()
    {
        _memoryCache.Remove(ActiveRubricCacheKey);

        // Note: We can't easily iterate over all cache keys to remove by prefix
        // In a production system, consider using a distributed cache with pattern matching
        // or tracking cache keys in a collection like ConnectivityCacheService does

        _logger.LogInformation("Invalidated rubric cache");
        return Task.CompletedTask;
    }

    public async Task<RubricConfiguration?> RefreshActiveRubricAsync(CancellationToken cancellationToken = default)
    {
        // Remove from cache
        _memoryCache.Remove(ActiveRubricCacheKey);
        _logger.LogDebug("Removed active rubric from cache, reloading from database");

        // Reload from database
        return await GetActiveRubricAsync(cancellationToken);
    }
}
