using Microsoft.AspNetCore.Http;
using NTC.QoS.Domain.Interfaces;
using System.Security.Claims;

namespace NTC.QoS.Infrastructure.Services;

public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor; // Inject IHttpContextAccessor

    public string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
        {
            return null; // Or handle the case where the user is not authenticated
        }

        // Extract user ID from JWT (adapt to your JWT claims)
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Example: Using NameIdentifier claim
    }

}
