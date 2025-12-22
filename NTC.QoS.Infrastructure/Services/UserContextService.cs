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

}
