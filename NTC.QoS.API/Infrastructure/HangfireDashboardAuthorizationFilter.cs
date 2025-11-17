using Hangfire.Dashboard;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.API.Infrastructure;

/// <summary>
/// Hangfire dashboard authorization filter that integrates with the existing role-based authorization system.
/// Uses the application's role keys and permission system for consistent security.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow in Development environment unconditionally for easier development
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return true;
        }

        // In production, require authentication
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check if user has system administration permission or is in authorized roles
        return HasSystemAdministrationAccess(httpContext);
    }

    /// <summary>
    /// Checks if the user has system administration access through roles or permissions
    /// </summary>
    private static bool HasSystemAdministrationAccess(HttpContext httpContext)
    {
        try
        {
            // Method 1: Check via role-based claims (if user roles are stored as claims)
            if (HasAuthorizedRole(httpContext))
            {
                return true;
            }

            // Method 2: Check via permission-based authorization (if implemented via claims)
            if (HasSystemAdministrationPermission(httpContext))
            {
                return true;
            }

            // Method 3: Database lookup (fallback if claims are not available)
            return HasDatabaseSystemAccess(httpContext);
        }
        catch (Exception)
        {
            // If any authorization check fails, deny access
            return false;
        }
    }

    /// <summary>
    /// Checks if user has authorized roles via claims
    /// </summary>
    private static bool HasAuthorizedRole(HttpContext httpContext)
    {
        // Check for authorized roles based on your RoleSeeder keys
        var authorizedRoles = new[] { "super_admin", "admin" };

        foreach (var role in authorizedRoles)
        {
            if (httpContext.User.IsInRole(role))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if user has system administration permission via claims
    /// </summary>
    private static bool HasSystemAdministrationPermission(HttpContext httpContext)
    {
        // Check for system administration permission claim
        return httpContext.User.HasClaim("permission", Permissions.SystemAdministration);
    }

    /// <summary>
    /// Database lookup for system access (fallback method)
    /// This method should be used sparingly as it involves database access
    /// </summary>
    private static bool HasDatabaseSystemAccess(HttpContext httpContext)
    {
        try
        {
            // Get the user identifier (could be email, auth_user_id, etc.)
            var userEmail = httpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return false;
            }

            // Get database context from DI
            using var scope = httpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if user exists and has authorized role
            var user = dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == userEmail && !u.IsDeleted);

            if (user?.Role == null)
            {
                return false;
            }

            // Check if user has authorized role
            var authorizedRoles = new[] { "super_admin", "admin" };
            return authorizedRoles.Contains(user.Role.Key);
        }
        catch (Exception)
        {
            // If database lookup fails, deny access
            return false;
        }
    }
}
