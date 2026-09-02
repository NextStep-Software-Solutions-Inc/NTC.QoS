using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring application startup logic in the Infrastructure layer
/// </summary>
public static class ApplicationStartupExtensions
{
    /// <summary>
    /// Configures and initializes the application startup processes including Hangfire filters and database operations
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="environment">The hosting environment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the startup operation</returns>
    public static async Task InitializeApplicationAsync(
        this IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        // Register global Hangfire filters (idempotent)
        serviceProvider.GetRequiredService<IHangfireFilterRegistrar>().RegisterGlobalFilters();

        // Conditional migrations & seeding
        var runMigrations = configuration.GetValue<bool?>("Database:RunMigrationsOnStartup") ?? environment.IsDevelopment();
        if (runMigrations)
        {
            // Run migrations and seeding in background to allow API to start immediately
            _ = Task.Run(async () =>
            {
                try
                {
                    await serviceProvider.ApplyMigrationsAndSeedAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var logger = serviceProvider.GetRequiredService<ILogger>();
                    logger.LogError(ex, "Background seeding failed");
                }
            }, cancellationToken);

            await Task.CompletedTask;
        }
    }
}