using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Interfaces;

namespace NTC.QoS.Application.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IMemoryCache memoryCache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if the current request is cachable
        if (request is ICacheableRequest cachableRequest)
        {
            var cacheKey = cachableRequest.CacheKey;

            // 1. Try to get from cache
            if (_memoryCache.TryGetValue(cacheKey, out var cachedResponse))
            {
                if (cachedResponse is TResponse response)
                {
                    _logger.LogInformation("Cache HIT: {CacheKey}", cacheKey);
                    return response;
                }
            }

            // 2. If not in cache, proceed to the next handler (e.g., the actual business logic)
            _logger.LogInformation("Cache MISS: {CacheKey}. Fetching from source.", cacheKey);
            TResponse responseFromSource = await next(); // This calls the actual request handler

            // 3. Cache the result
            var cacheEntryOptions = new MemoryCacheEntryOptions();

            if (cachableRequest.AbsoluteExpirationRelativeToNow.HasValue)
            {
                cacheEntryOptions.SetAbsoluteExpiration(cachableRequest.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (cachableRequest.SlidingExpiration.HasValue)
            {
                cacheEntryOptions.SetSlidingExpiration(cachableRequest.SlidingExpiration.Value);
            }
            else
            {
                // Default expiration if none specified
                cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromHours(24));
            }

            _memoryCache.Set(cacheKey, responseFromSource, cacheEntryOptions);
            _logger.LogInformation("Cache SET: {CacheKey}", cacheKey);

            return responseFromSource;
        }

        // If the request is not cachable, just pass it through
        return await next();
    }
}
