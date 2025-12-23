using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for reporting service registration
/// </summary>
internal static class ReportingServiceExtensions
{
    /// <summary>
    /// Registers reporting services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReportingServices(this IServiceCollection services)
    {
        services.AddScoped<IReportGenerationService, ReportGenerationService>();
        services.AddScoped<IProviderTechnologyService, ProviderTechnologyService>();
        services.AddScoped<IWirelessScoringService, WirelessScoringService>();
        return services;
    }
}
