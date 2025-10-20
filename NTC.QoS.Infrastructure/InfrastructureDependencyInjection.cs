using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Extensions;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed;
using NTC.QoS.Infrastructure.Services;

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

        // Register ClerkUserService for user management
        services.AddHttpClient<IClerkUserService, ClerkUserService>(client =>
        {
            // BaseAddress and DefaultRequestHeaders should ideally be set here
            // to ensure the HttpClient instance injected into ClerkService is pre-configured.
            // This also ensures that the HttpClient is correctly managed by HttpClientFactory.
            client.BaseAddress = new Uri("https://api.clerk.com/v1/");
            // The API key will be added in the ClerkUserService constructor as it depends on IConfiguration.
            // For a more robust solution, consider passing Clerk API settings via options pattern.
        });

        // Register services in logical groups
        services.AddDatabaseServices(configuration);
        services.AddHangfireServices(configuration);
        services.AddExternalServices(configuration);
        services.AddBlobStorageServices(configuration);
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
        var configuration = services.GetRequiredService<IConfiguration>();
        var databaseOptions = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        var advisoryLockKey = databaseOptions.AdvisoryLockKey;
        var lockAcquired = false;

        try
        {
            // Seed initial data
            logger.LogInformation("Starting data seeding process...");
            await SeedInitialDataAsync(dbContext, services, configuration, logger, cancellationToken);
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
    private static async Task SeedInitialDataAsync(ApplicationDbContext dbContext, IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken)
    {
        var blobStorageService = serviceProvider.GetRequiredService<IBlobStorageService>();
        var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        // Apply migrations first
        await dbContext.Database.MigrateAsync(cancellationToken);

        var seeders = new (string Name, Func<Task> SeedAction)[]
        {
            ("User Data", async () => {
                // Try to get ClerkUserService, but don't fail if it's not available
                IClerkUserService? clerkUserService = null;
                try
                {
                    clerkUserService = serviceProvider.GetService<IClerkUserService>();
                }
                catch (Exception ex)
                {
                    logger.LogWarning("ClerkUserService not available during seeding: {Message}. User seeding will proceed without Clerk integration.", ex.Message);
                }
                
                await UserSeeder.SeedAsync(dbContext, clerkUserService, configuration, logger);
            }),
            ("Location Data", () => LocationSeeder.SeedAllAsync(dbContext, blobStorageService, logger, databaseOptions.ForceReseed)),
            ("Organization Data", () => OrganizationSeeder.SeedAsync(dbContext, logger)),
            ("Role Data", () => RoleSeeder.SeedAsync(dbContext, logger)),
            ("Authorization Data", () => AuthorizationSeeder.SeedAsync(dbContext, logger))
        };
        
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
