using NTC.QoS.API.Middleware;
using NTC.QoS.Domain.Models.Logging;

namespace NTC.QoS.API.Extensions;

/// <summary>
/// Extension methods for registering logging middleware and services.
/// </summary>
public static class LoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds verbose API logging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVerboseApiLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind logging settings from configuration
        services.Configure<VerboseApiLoggingSettings>(
            configuration.GetSection("VerboseApiLogging"));

        return services;
    }

    /// <summary>
    /// Adds the RequestResponseLoggingMiddleware to the application pipeline.
    /// This middleware logs all API requests and responses with comprehensive details.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseVerboseApiLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
