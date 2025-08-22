using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetProviderByIdHandler(
    IRepository<Domain.Entities.Provider> repository,
    IMemoryCache memoryCache,
    ILogger<GetProviderByIdHandler> logger) : IRequestHandler<GetProviderByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<GetProviderByIdHandler> _logger = logger;

    // Cache configuration
    private static readonly MemoryCacheEntryOptions LogoCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30), // Cache for 30 minutes
        SlidingExpiration = TimeSpan.FromMinutes(10), // Refresh if accessed within 10 minutes
        Priority = CacheItemPriority.Normal,
        Size = 1 // Each cache entry counts as 1 unit
    };

    public async Task<ApiResponse<ProviderDto>> Handle(GetProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var providerQuery = _repository.Query(p => p.Id.Equals(request.Id));
        var provider = await _repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is null)
        {
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvider", [ "Provider not found."]
                    }
                },
                Message = "The provider does not exist."
            };
        }

        string? logoDataBase64 = null;
        
        // Only process logo data if requested (defaults to true for backward compatibility)
        var shouldIncludeLogo = request.IncludeLogoData ?? true;
        
        if (shouldIncludeLogo && provider.LogoData != null && provider.LogoData.Length > 0)
        {
            // Create cache key based on provider ID and logo upload timestamp
            var cacheKey = $"provider_logo_b64_{provider.Id}_{provider.LogoUploadedAt?.Ticks ?? 0}";
            
            // Try to get from cache first
            if (!_memoryCache.TryGetValue(cacheKey, out logoDataBase64))
            {
                // Cache miss - convert to base64 and cache the result
                _logger.LogDebug("Converting logo to base64 for provider {ProviderId}, size: {Size} bytes", 
                    provider.Id, provider.LogoData.Length);
                
                try
                {
                    logoDataBase64 = Convert.ToBase64String(provider.LogoData);
                    
                    // Cache the base64 string for future requests
                    _memoryCache.Set(cacheKey, logoDataBase64, LogoCacheOptions);
                    
                    _logger.LogDebug("Cached base64 logo for provider {ProviderId}", provider.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert logo to base64 for provider {ProviderId}", provider.Id);
                    logoDataBase64 = null;
                }
            }
            else
            {
                _logger.LogDebug("Retrieved cached base64 logo for provider {ProviderId}", provider.Id);
            }
        }
        else if (!shouldIncludeLogo)
        {
            _logger.LogDebug("Logo data excluded from response for provider {ProviderId} per request", provider.Id);
        }

        return new ApiResponse<ProviderDto>
        {
            Succeeded = true,
            Message = "Provider retrieved successfully.",
            Data = new ProviderDto()
            {
                Id = provider.Id,
                Name = provider.Name,
                Description = provider.Description,
                LicenseNumber = provider.LicenseNumber,
                Color = provider.Color,
                HasLogo = provider.LogoData != null && provider.LogoData.Length > 0,
                LogoContentType = provider.LogoContentType,
                LogoFileName = provider.LogoFileName,
                LogoSizeBytes = provider.LogoData?.Length,
                LogoUploadedAt = provider.LogoUploadedAt,
                LogoDataBase64 = logoDataBase64
            }
        };
    }
}
