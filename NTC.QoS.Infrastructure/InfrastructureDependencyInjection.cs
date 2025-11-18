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
using System.Security.Cryptography;
using System.Text;

namespace NTC.QoS.Infrastructure;

public static class InfrastructureDependencyInjection
{
    // Default advisory lock identifier - can be overridden via configuration
    private const string DefaultAdvisoryLockIdentifier = "migration-advisory-lock";

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

        // Get configurable advisory lock identifier
        var lockIdentifier = configuration.GetValue<string>("Database:AdvisoryLockIdentifier") ?? DefaultAdvisoryLockIdentifier;
        var advisoryLockKey = GenerateAdvisoryLockKey(lockIdentifier);
        var lockAcquired = false;

        try
        {
            // Attempt to acquire advisory lock to prevent concurrent seeding
            logger.LogInformation("Attempting to acquire advisory lock {LockKey} for identifier '{Identifier}'...",
                advisoryLockKey, lockIdentifier);
            lockAcquired = await TryAcquireAdvisoryLockAsync(dbContext, advisoryLockKey, logger, cancellationToken);

            if (!lockAcquired)
            {
                logger.LogWarning("Could not acquire advisory lock {LockKey}. Another instance may be running migrations. Skipping seeding.", advisoryLockKey);
                return;
            }

            logger.LogInformation("Advisory lock {LockKey} acquired successfully", advisoryLockKey);

            // Seed initial data
            logger.LogInformation("Starting data seeding process...");
            await SeedInitialDataAsync(dbContext, services, logger, cancellationToken);
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
    /// Attempts to acquire a PostgreSQL advisory lock using parameterized query
    /// </summary>
    /// <returns>True if lock was acquired, false otherwise</returns>
    private static async Task<bool> TryAcquireAdvisoryLockAsync(ApplicationDbContext dbContext, long advisoryLockKey, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            // Use SqlQueryRaw with parameters to prevent SQL injection
            var lockResult = await dbContext.Database
                .SqlQueryRaw<bool>("SELECT pg_try_advisory_lock({0}) AS \"Value\"", advisoryLockKey)
                .FirstOrDefaultAsync(cancellationToken);

            return lockResult;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to acquire advisory lock {LockKey}. Error: {ErrorMessage}", advisoryLockKey, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Seeds initial data in the correct order with error handling for each seeder.
    /// Uses the optimized LocationSeederV2 by default, falls back to LocationSeeder if needed.
    /// </summary>
    private static async Task SeedInitialDataAsync(ApplicationDbContext dbContext, IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken)
    {
        var blobStorageService = serviceProvider.GetRequiredService<IBlobStorageService>();
        var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Use configuration to determine which seeder version to use
        var useOptimizedSeeder = databaseOptions.UseOptimizedLocationSeeder ?? true;

        var seeders = new (string Name, Func<Task> SeedAction)[]
        {
            ("Location Data", async () =>
            {
                if (useOptimizedSeeder)
                {
                    logger.LogInformation("Using optimized LocationSeederV2 with streaming architecture");
                    var seederV2 = new LocationSeederV2(dbContext, blobStorageService,
                        loggerFactory.CreateLogger<LocationSeederV2>(), loggerFactory);
                    await seederV2.SeedAllAsync(databaseOptions.ForceReseed, cancellationToken);
                }
                else
                {
                    logger.LogInformation("Using legacy LocationSeeder");
                    await LocationSeeder.SeedAllAsync(dbContext, blobStorageService, logger, databaseOptions.ForceReseed);
                }
            }),
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
            logger.LogInformation("Releasing advisory lock {LockKey}", advisoryLockKey);
            // Use ExecuteSqlRaw which doesn't expect a return value
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0})",
                [advisoryLockKey],
                cancellationToken
            );
            logger.LogInformation("Advisory lock {LockKey} released successfully", advisoryLockKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to release advisory lock {LockKey}. This may cause issues if the application restarts quickly", advisoryLockKey);
            // Don't re-throw as this is cleanup code
        }
    }

    /// <summary>
    /// Generates a consistent advisory lock key using a hash of a configurable string.
    /// This allows different applications sharing the same database to use different locks.
    /// </summary>
    /// <param name="identifier">The string identifier to hash (e.g., "migration-advisory-lock" or "app1-migration-lock")</param>
    /// <returns>A 64-bit advisory lock key</returns>
    private static long GenerateAdvisoryLockKey(string identifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
        return BitConverter.ToInt64(hash, 0);
    }
}
