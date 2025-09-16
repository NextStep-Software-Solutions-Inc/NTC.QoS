using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Analytics;

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
    private static readonly Dictionary<string, List<string>> _cacheKeyPatterns = new();

    public ConnectivityCacheService(
        IMemoryCache memoryCache,
        IOptions<ConnectivityCacheOptions> options,
        ILogger<ConnectivityCacheService> logger)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
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
            Priority = GetCachePriorityForType<T>()
        };

        _memoryCache.Set(cacheKey, data, options);
        
        // Track cache keys for pattern-based invalidation
        TrackCacheKey(cacheKey);
        
        await Task.CompletedTask;
    }

    public async Task InvalidateCacheAsync(string pattern)
    {
        if (_cacheKeyPatterns.TryGetValue(pattern, out var keys))
        {
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }
            _cacheKeyPatterns[pattern].Clear();
            
            _logger.LogInformation("Invalidated {Count} cache entries for pattern {Pattern}", keys.Count, pattern);
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
        if (!_cacheKeyPatterns.ContainsKey(pattern))
        {
            _cacheKeyPatterns[pattern] = new List<string>();
        }
        _cacheKeyPatterns[pattern].Add(cacheKey);
    }
}