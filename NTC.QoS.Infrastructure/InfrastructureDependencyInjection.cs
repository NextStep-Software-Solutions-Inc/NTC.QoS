using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Extensions;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed;

namespace NTC.QoS.Infrastructure;

public static class InfrastructureDependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services with improved organization and configuration support
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register logging first
        services.AddLogging();

        // Register services in logical groups
        services.AddDatabaseServices(configuration);
        services.AddHangfireServices(configuration);
        services.AddExternalServices(configuration);
        services.AddRepositories();
        services.AddBackgroundJobServices();

        return services;
    }

    /// <summary>
    /// Applies database migrations and seeds initial data with enhanced error handling and configuration support
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the migration and seeding operation</returns>
    public static async Task ApplyMigrationsAndSeedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var databaseOptions = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        var advisoryLockKey = databaseOptions.AdvisoryLockKey;
        var lockAcquired = false;

        try
        {
            // Seed initial data
            logger.LogInformation("Starting data seeding process...");
            await SeedInitialDataAsync(dbContext, logger, cancellationToken);
            logger.LogInformation("Data seeding completed successfully");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Migration and seeding process was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply migrations and seed data");
            throw;
        }
        finally
        {
            if (lockAcquired)
            {
                await ReleaseAdvisoryLockAsync(dbContext, advisoryLockKey, logger, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Seeds initial data in the correct order with error handling for each seeder
    /// </summary>
    private static async Task SeedInitialDataAsync(ApplicationDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var seeders = new (string Name, Func<Task> SeedAction)[]
        {
            ("Location Data", () => LocationSeeder.SeedAllAsync(dbContext, logger)),
            ("Organization Data", () => OrganizationSeeder.SeedAsync(dbContext, logger)),
            ("Role Data", () => RoleSeeder.SeedAsync(dbContext, logger)),
            ("Authorization Data", () => AuthorizationSeeder.SeedAsync(dbContext, logger))
        };

        // Apply migrations first
        await dbContext.Database.MigrateAsync(cancellationToken);
        
        // Use the seeders array instead of calling them directly
        foreach (var (name, seedAction) in seeders)
        {
            try
            {
                logger.LogInformation("Starting {SeederName}...", name);
                await seedAction();
                logger.LogInformation("Completed {SeederName}", name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed {SeederName}", name);
                throw;
            }
        }
    }

    /// <summary>
    /// Safely releases the PostgreSQL advisory lock
    /// </summary>
    private static async Task ReleaseAdvisoryLockAsync(ApplicationDbContext dbContext, long advisoryLockKey, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Releasing advisory lock {LockKey}", advisoryLockKey);
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0});", 
                new object[] { advisoryLockKey }, 
                cancellationToken
            );
            logger.LogDebug("Advisory lock {LockKey} released successfully", advisoryLockKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to release advisory lock {LockKey}. This may cause issues if the application restarts quickly", advisoryLockKey);
            // Don't re-throw as this is cleanup code
        }
    }
}
