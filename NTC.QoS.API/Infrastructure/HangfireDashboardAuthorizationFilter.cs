using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace NTC.QoS.API.Infrastructure;

/// <summary>
/// Simple Hangfire dashboard authorization filter.
/// In production you can replace logic with role/claim/permission checks.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // Allow in Development unconditionally; tighten in prod.
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return true;

        // Example: only allow authenticated users with an admin claim.
        if (httpContext.User?.Identity?.IsAuthenticated == true &&
            httpContext.User.IsInRole("Administrator"))
        {
            return true;
        }

        return false;
    }
}
