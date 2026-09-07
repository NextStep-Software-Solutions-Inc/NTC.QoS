using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for external service registration
/// </summary>
internal static class ExternalServiceExtensions
{
    /// <summary>
    /// Registers external services (HTTP clients, third-party services)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HTTP client for Clerk user service
        services.AddHttpClient<IClerkUserService, ClerkUserService>(client =>
        {
            client.BaseAddress = new Uri("https://api.clerk.com/v1/");
            var clerkSecretKey = configuration["Clerk:SecretKey"];
            if (!string.IsNullOrWhiteSpace(clerkSecretKey))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", clerkSecretKey);
            }
            // Add timeout configuration
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Core services
        services.AddScoped<IUserContextService, UserContextService>();
        
        // Register CellularCoverage mapping services (standard and V2)
        services.AddScoped<ICellularCoverageMappingService, CellularCoverageMappingService>();
        services.AddScoped<ICellularCoverageV2MappingService, CellularCoverageV2MappingService>();

        // Google Generative AI configuration and service
        services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
        services.AddScoped<IGenAIService, GenAIService>();
        
        // AI Instruction service (loads global AI context/instruction)
        services.AddSingleton<IAIInstructionService, AIInstructionService>();

        return services;
    }
}