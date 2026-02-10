using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using System.Security.Claims;

namespace NTC.QoS.Infrastructure.Services;

public class UserContextService(IHttpContextAccessor httpContextAccessor, ILogger<UserContextService> logger) : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<UserContextService> _logger = logger;

    public string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
        {
            _logger.LogWarning("User context: No authenticated user found");
            return null;
        }

        // Extract user ID from JWT (Clerk's 'sub' claim is mapped to ClaimTypes.NameIdentifier via NameClaimType in Program.cs)
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User is authenticated but ClaimTypes.NameIdentifier is missing. Available claims: {Claims}",
                string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}")));
        }
        
        return userId;
    }

    public Guid? GetCurrentRegionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("User context: No HTTP context available for region ID extraction");
            return null;
        }

        // Try to get region ID from X-Region-Id header
        if (httpContext.Request.Headers.TryGetValue("X-Region-Id", out var regionIdHeader))
        {
            if (Guid.TryParse(regionIdHeader.ToString(), out var regionId))
            {
                _logger.LogDebug("Region context: Extracted region ID {RegionId} from X-Region-Id header", regionId);
                return regionId;
            }
            else
            {
                _logger.LogWarning("Region context: Invalid region ID format in X-Region-Id header: {Value}", regionIdHeader.ToString());
            }
        }

        // Try to get from query string as fallback
        if (httpContext.Request.Query.TryGetValue("regionId", out var regionIdQuery))
        {
            if (Guid.TryParse(regionIdQuery.ToString(), out var regionId))
            {
                _logger.LogDebug("Region context: Extracted region ID {RegionId} from query string", regionId);
                return regionId;
            }
        }

        _logger.LogDebug("Region context: No region ID found in request context");
        return null;
    }

}
