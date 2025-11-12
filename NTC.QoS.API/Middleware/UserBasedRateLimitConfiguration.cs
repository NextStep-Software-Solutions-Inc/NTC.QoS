using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace NTC.QoS.API.Middleware;

/// <summary>
/// Custom client ID resolver that extracts user ID from JWT claims
/// </summary>
public class UserIdResolveContributor : IClientResolveContributor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIdResolveContributor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string> ResolveClientAsync(HttpContext httpContext)
    {
        // Try to get user ID from claims (NameIdentifier claim contains the user ID)
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // If user is not authenticated, fall back to IP address
        if (string.IsNullOrEmpty(userId))
        {
            userId = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        return Task.FromResult(userId);
    }
}