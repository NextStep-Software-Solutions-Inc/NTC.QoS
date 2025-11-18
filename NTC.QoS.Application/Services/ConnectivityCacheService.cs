using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using System.Collections.Concurrent;

namespace NTC.QoS.Application.Services;

public interface IConnectivityCacheService
{
    Task<T?> GetCachedResultAsync<T>(string cacheKey) where T : class;
    Task SetCachedResultAsync<T>(string cacheKey, T data, TimeSpan? expiry = null) where T : class;
    Task InvalidateCacheAsync(string pattern);
    string GenerateInfrastructureCacheKey(Guid locationId, LocationTypeEnum locationType);
    string GenerateSpeedStatsCacheKey(Guid locationId, LocationTypeEnum locationType);
    string GenerateTechnologyCacheKey(Guid locationId, LocationTypeEnum locationType);
    string GenerateAssessmentCacheKey(Guid locationId, LocationTypeEnum locationType);
}

public class ConnectivityCacheOptions
{
    public int InfrastructureCacheHours { get; set; } = 24;
    public int SpeedStatsCacheHours { get; set; } = 4;
    public int TechnologyCacheHours { get; set; } = 12;
    public int AssessmentCacheHours { get; set; } = 8;
    public int MaxCacheSizeMB { get; set; } = 200;
}

public class ConnectivityCacheService : IConnectivityCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConnectivityCacheOptions _options;
    private readonly ILogger<ConnectivityCacheService> _logger;
    // Use ConcurrentDictionary for thread-safe cache key pattern tracking
    private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _cacheKeyPatterns = new();

    public ConnectivityCacheService(
        IMemoryCache memoryCache,
        IOptions<ConnectivityCacheOptions> options,
        ILogger<ConnectivityCacheService> logger)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;

        // Note: MemoryCache size limits must be configured at DI registration time,
        // not in the constructor. The size limits are now configured in ApplicationDependencyInjection.cs
    }

    public async Task<T?> GetCachedResultAsync<T>(string cacheKey) where T : class
    {
        return await Task.FromResult(_memoryCache.Get<T>(cacheKey));
    }

    public async Task SetCachedResultAsync<T>(string cacheKey, T data, TimeSpan? expiry = null) where T : class
    {
        var cacheExpiry = expiry ?? GetCacheExpiryForType<T>();

        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = cacheExpiry,
            Priority = GetCachePriorityForType<T>(),
            // Set size for entries to make MaxCacheSizeMB effective
            Size = EstimateObjectSize<T>()
        };

        _memoryCache.Set(cacheKey, data, options);

        // Track cache keys for pattern-based invalidation (now thread-safe)
        TrackCacheKey(cacheKey);

        await Task.CompletedTask;
    }

    public async Task InvalidateCacheAsync(string pattern)
    {
        // Thread-safe access to cache key patterns
        if (_cacheKeyPatterns.TryGetValue(pattern, out var keyBag))
        {
            var keys = keyBag.ToArray(); // Get snapshot of keys
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }

            // Clear the bag (thread-safe operation)
            _cacheKeyPatterns.TryUpdate(pattern, [], keyBag);

            _logger.LogInformation("Invalidated {Count} cache entries for pattern {Pattern}", keys.Length, pattern);
        }
        await Task.CompletedTask;
    }

    public string GenerateInfrastructureCacheKey(Guid locationId, LocationTypeEnum locationType)
        => $"infra:{locationType}:{locationId}";

    public string GenerateSpeedStatsCacheKey(Guid locationId, LocationTypeEnum locationType)
        => $"speed:{locationType}:{locationId}";

    public string GenerateTechnologyCacheKey(Guid locationId, LocationTypeEnum locationType)
        => $"tech:{locationType}:{locationId}";

    public string GenerateAssessmentCacheKey(Guid locationId, LocationTypeEnum locationType)
        => $"assessment:{locationType}:{locationId}";

    private TimeSpan GetCacheExpiryForType<T>()
    {
        return typeof(T).Name switch
        {
            nameof(InfrastructureStatsDto) => TimeSpan.FromHours(_options.InfrastructureCacheHours),
            nameof(SpeedStatsDto) => TimeSpan.FromHours(_options.SpeedStatsCacheHours),
            nameof(TechnologyBreakdownDto) => TimeSpan.FromHours(_options.TechnologyCacheHours),
            nameof(ConnectivityAssessment) => TimeSpan.FromHours(_options.AssessmentCacheHours),
            _ => TimeSpan.FromHours(1)
        };
    }

    private CacheItemPriority GetCachePriorityForType<T>()
    {
        return typeof(T).Name switch
        {
            nameof(ConnectivityAssessment) => CacheItemPriority.High,
            nameof(InfrastructureStatsDto) => CacheItemPriority.Normal,
            nameof(SpeedStatsDto) => CacheItemPriority.Normal,
            _ => CacheItemPriority.Low
        };
    }

    private void TrackCacheKey(string cacheKey)
    {
        var pattern = cacheKey.Split(':')[0];
        _cacheKeyPatterns.AddOrUpdate(
            pattern,
            [cacheKey],
            (key, existing) =>
            {
                existing.Add(cacheKey);
                return existing;
            });
    }

    // Add size estimation for cache entries
    private long EstimateObjectSize<T>()
    {
        return typeof(T).Name switch
        {
            nameof(ConnectivityAssessment) => 5, // 5KB estimated
            nameof(InfrastructureStatsDto) => 2, // 2KB estimated
            nameof(SpeedStatsDto) => 1, // 1KB estimated
            nameof(TechnologyBreakdownDto) => 1, // 1KB estimated
            _ => 1 // Default 1KB
        };
    }
}