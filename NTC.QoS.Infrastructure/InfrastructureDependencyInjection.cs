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
using NTC.QoS.Infrastructure.Seed.Interfaces;

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
        services.AddBlobStorageServices(configuration);
        services.AddRepositories();
        services.AddBackgroundJobServices();
        services.AddSeedServices();

        return services;
    }

    /// <summary>
    /// Registers seed services for data seeding
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    private static IServiceCollection AddSeedServices(this IServiceCollection services)
    {
        services.AddScoped<IUserSeeder, NTC.QoS.Infrastructure.Seed.Services.UserSeeder>();
        services.AddScoped<IConfigurationValidator, NTC.QoS.Infrastructure.Seed.Services.ConfigurationValidator>();
        services.AddScoped<IPasswordGenerator, NTC.QoS.Infrastructure.Seed.Services.PasswordGenerator>();
        services.AddScoped<IUserCreator, NTC.QoS.Infrastructure.Seed.Services.UserCreator>();
        services.AddScoped<IUserUpdater, NTC.QoS.Infrastructure.Seed.Services.UserUpdater>();
        return services;
    }

    /// <summary>
    /// Applies database migrations and seeds initial data with enhanced error handling and configuration support</summary>
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
            // Try to acquire advisory lock to coordinate migrations across multiple instances
            logger.LogInformation("Attempting to acquire advisory lock {LockKey} for migrations...", advisoryLockKey);
            lockAcquired = await TryAcquireAdvisoryLockAsync(dbContext, advisoryLockKey, logger, cancellationToken);

            if (!lockAcquired)
            {
                logger.LogWarning("Could not acquire advisory lock {LockKey}. Another instance may be running migrations. Skipping migration and seeding.", advisoryLockKey);
                return;
            }

            logger.LogInformation("Advisory lock {LockKey} acquired successfully", advisoryLockKey);

            // Apply migrations
            logger.LogInformation("Starting database migrations...");
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations completed successfully");

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
    /// Attempts to acquire PostgreSQL advisory lock for migration coordination
    /// </summary>
    /// <returns>True if lock was acquired, false if another instance holds the lock</returns>
    private static async Task<bool> TryAcquireAdvisoryLockAsync(ApplicationDbContext dbContext, long advisoryLockKey, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Executing pg_try_advisory_lock for key {LockKey}", advisoryLockKey);
            
            // pg_try_advisory_lock returns true if lock was acquired, false if already held
            // We need to alias the result as "Value" for EF Core to map it correctly
            // Using SingleAsync since we expect exactly one boolean result
            var lockResult = await dbContext.Database
                .SqlQueryRaw<bool>($"SELECT pg_try_advisory_lock({advisoryLockKey}) AS \"Value\"")
                .SingleAsync(cancellationToken);

            if (lockResult)
            {
                logger.LogDebug("Successfully acquired advisory lock {LockKey}", advisoryLockKey);
            }
            else
            {
                logger.LogDebug("Advisory lock {LockKey} is already held by another instance", advisoryLockKey);
            }

            return lockResult;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to acquire advisory lock {LockKey}. Proceeding without lock.", advisoryLockKey);
            // If we can't acquire the lock due to an error, we should NOT proceed with migrations
            // to avoid potential conflicts
            return false;
        }
    }

    /// <summary>
    /// Seeds initial data in the correct order with error handling for each seeder
    /// </summary>
    private static async Task SeedInitialDataAsync(ApplicationDbContext dbContext, IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken)
    {
        var blobStorageService = serviceProvider.GetRequiredService<IBlobStorageService>();
        var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        var seeders = new (string Name, Func<Task> SeedAction, bool IsCritical)[]
        {
            ("Organization Data", () => OrganizationSeeder.SeedAsync(dbContext, logger), true),
            ("Role Data", () => RoleSeeder.SeedAsync(dbContext, logger), true),
            ("User Data", async () => {
                var userSeeder = serviceProvider.GetRequiredService<IUserSeeder>();
                await userSeeder.SeedAsync();
            }, true),
            ("Authorization Data", () => AuthorizationSeeder.SeedAsync(dbContext, logger), true),
            ("Location Data", () => LocationSeeder.SeedAllAsync(dbContext, blobStorageService, logger, databaseOptions.ForceReseed), false)
        };
        
        // Use the seeders array
        foreach (var (name, seedAction, isCritical) in seeders)
        {
            try
            {
                logger.LogInformation("Starting {SeederName}...", name);
                await seedAction();
                logger.LogInformation("Completed {SeederName}", name);
            }
            catch (Exception ex)
            {
                if (isCritical)
                {
                    logger.LogCritical(ex, "CRITICAL ERROR: Failed to seed {SeederName}. Application cannot start without this data. Error: {ErrorMessage}", 
                        name, ex.Message);
                    // Re-throw to terminate application startup
                    throw;
                }
                else
                {
                    logger.LogError(ex, "Failed to seed {SeederName}. Application will continue but some features may not work correctly. Error: {ErrorMessage}", 
                        name, ex.Message);
                }
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
