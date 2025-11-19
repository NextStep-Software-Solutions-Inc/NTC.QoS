using NTC.QoS.API.Middleware;

namespace NTC.QoS.API.Extensions;

/// <summary>
/// Extension methods for registering security middleware.
/// </summary>
public static class SecurityMiddlewareExtensions
{
    /// <summary>
    /// Adds the SecurityHeadersMiddleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Configures production security middleware including HSTS and HTTPS redirection.
    /// This method should be called when the application is running in production mode.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseProductionSecurity(this IApplicationBuilder app)
    {
        app.UseHsts();
        app.UseHttpsRedirection();

        return app;
    }
}