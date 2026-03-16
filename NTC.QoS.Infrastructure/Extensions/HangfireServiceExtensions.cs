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

            // Increase shutdown timeout to allow long-running jobs to complete gracefully
            // Default is 15 seconds, which is too short for long-running spatial analysis jobs
            options.ShutdownTimeout = TimeSpan.FromMinutes(5);

            // Increase cancellation timeout to allow database operations to complete
            options.CancellationCheckInterval = TimeSpan.FromSeconds(30);

            // CRITICAL: Set worker stop timeout to prevent 30-minute job cancellation
            // Default is 30 minutes which causes long-running jobs to be cancelled
            // Set to a very high value to allow multi-hour processing jobs
            options.StopTimeout = TimeSpan.FromHours(6);

            // Increase schedule polling interval to reduce database load
            options.SchedulePollingInterval = TimeSpan.FromMinutes(1);

            // Explicitly list queues so the main server never picks up "import" jobs.
            // Import jobs are handled exclusively by the dedicated single-worker import server
            // to prevent concurrent large bulk inserts from exhausting the DB connection pool.
            options.Queues = new[] { "default", "report-generation", "connectivity-assessment", "seed-processing" };
        });

        // Dedicated import server — 1 worker prevents concurrent large imports from exhausting
        // the DB connection pool during bulk insert operations (NpgsqlConnection pool exhaustion).
        services.AddHangfireServer((_, importOptions) =>
        {
            importOptions.WorkerCount = 1;
            importOptions.ServerName = $"{GenerateServerName()}-import";
            importOptions.Queues = new[] { "import" };
            importOptions.ShutdownTimeout = TimeSpan.FromMinutes(5);
            importOptions.StopTimeout = TimeSpan.FromHours(6);
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