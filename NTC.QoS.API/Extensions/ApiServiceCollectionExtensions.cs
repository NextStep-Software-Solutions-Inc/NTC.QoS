using AspNetCoreRateLimit;
using NTC.QoS.API.Middleware;

namespace NTC.QoS.API.Extensions;

/// <summary>
/// Extension methods for configuring API-specific services.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds rate limiting services to the service collection.
    /// Configures client-based rate limiting with 100 requests per 10 seconds per user.
    /// </summary>
    /// <param name="services">The service collection to add rate limiting to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        // Rate limiting configuration
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        // Configure client-based rate limiting (per user)
        services.Configure<ClientRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.HttpStatusCode = 429;
            options.ClientIdHeader = "X-ClientId";
            options.GeneralRules =
            [
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "10s",
                    Limit = 1000,
                }
            ];
        });

        // Register custom client resolver for user-based rate limiting
        services.AddSingleton<IClientResolveContributor, UserIdResolveContributor>();

        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

        return services;
    }
}