using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for Hangfire service registration
/// </summary>
internal static class HangfireServiceExtensions
{
    /// <summary>
    /// Registers Hangfire services with configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Hangfire options with defaults
        services.Configure<HangfireOptions>(hangfireOptions =>
        {
            var section = configuration.GetSection(HangfireOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(hangfireOptions);
            }
            // Defaults are already set in HangfireOptions class
        });
        
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSqlConnection string is required for Hangfire. " +
                "Use User Secrets for development or Environment Variables for production.");
        }

        // Register Hangfire with configuration
        services.AddHangfire((serviceProvider, cfg) =>
        {
            var hangfireOptions = serviceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;
            
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(s => s.UseNpgsqlConnection(connectionString));

            if (hangfireOptions.EnableDashboardMetrics)
            {
                cfg.UseDashboardMetrics();
            }
        });

        // Register Hangfire server with configuration
        services.AddHangfireServer((serviceProvider, options) =>
        {
            var hangfireOptions = serviceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;
            
            // Configure worker count
            int fallback = Math.Min(Environment.ProcessorCount, hangfireOptions.MaxWorkerCount);
            options.WorkerCount = hangfireOptions.WorkerCount is > 0 
                ? Math.Min(hangfireOptions.WorkerCount.Value, hangfireOptions.MaxWorkerCount) 
                : fallback;

            // Configure server name
            options.ServerName = hangfireOptions.ServerName ?? GenerateServerName();
        });

        // Register Hangfire filter registrar
        services.AddSingleton<IHangfireFilterRegistrar, HangfireFilterRegistrar>();

        return services;
    }

    private static string GenerateServerName()
    {
        var appName = Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME")
                     ?? AppDomain.CurrentDomain.FriendlyName
                     ?? "HangfireServer";
        return $"{appName}-{Environment.MachineName}";
    }
}