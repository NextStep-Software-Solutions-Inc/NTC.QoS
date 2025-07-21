using Microsoft.Extensions.Caching.Memory;
namespace NTC.QoS.API.Services
{
    public class PublicCachePurgeService : BackgroundService
    {
        private readonly IMemoryCache _memoryCache;
        private const string PublicCacheKeysSet = "public_cache_keys";

        public PublicCachePurgeService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                PurgePublicCache();
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private void PurgePublicCache()
        {
            if(_memoryCache.TryGetValue<HashSet<string>>(PublicCacheKeysSet, out var keys) && keys != null)
            {
                foreach (var key in keys)
                {
                    _memoryCache.Remove(key);
                }
            }
        }
    }
}
