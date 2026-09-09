using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
    /// Configures and initializes the application startup processes including Hangfire filters, CORS, and database operations
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

        // Configure Blob Storage CORS rules (for Azurite emulator & Azure Blob direct browser uploads)
        await serviceProvider.ConfigureBlobStorageCorsAsync(configuration, environment, cancellationToken);
        await serviceProvider.EnsureBlobContainersExistAsync(environment, cancellationToken);

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
                    var logger = serviceProvider.GetRequiredService<ILogger<IHangfireFilterRegistrar>>();
                    logger.LogError(ex, "Background seeding failed");
                }
            }, cancellationToken);

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Configures CORS rules on Azure Blob Storage / Azurite emulator to enable direct browser uploads via SAS URLs
    /// </summary>
    public static async Task ConfigureBlobStorageCorsAsync(
        this IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = serviceProvider.GetService<BlobServiceClient>();
            if (blobServiceClient == null) return;

            var logger = serviceProvider.GetRequiredService<ILogger<BlobServiceClient>>();

            var allowedOrigins = environment.IsDevelopment()
                ? "*"
                : string.Join(",", configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"]);

            var propertiesResponse = await blobServiceClient.GetPropertiesAsync(cancellationToken);
            var properties = propertiesResponse.Value;

            properties.Cors.Clear();
            properties.Cors.Add(new BlobCorsRule
            {
                AllowedOrigins = string.IsNullOrWhiteSpace(allowedOrigins) ? "*" : allowedOrigins,
                AllowedMethods = "GET,PUT,POST,OPTIONS,HEAD,DELETE,MERGE",
                AllowedHeaders = "*",
                ExposedHeaders = "*",
                MaxAgeInSeconds = 3600
            });

            await blobServiceClient.SetPropertiesAsync(properties, cancellationToken);
            logger.LogInformation("Blob Storage CORS rules configured successfully for origins: {Origins}", allowedOrigins);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<BlobServiceClient>>();
            logger.LogWarning(ex, "Failed to configure Blob Storage CORS rules. Pre-signed uploads might fail if CORS is not configured on the storage account.");
        }
    }

    /// <summary>
    /// Ensures standard blob containers exist on application startup
    /// </summary>
    public static async Task EnsureBlobContainersExistAsync(
        this IServiceProvider serviceProvider,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobServiceClient = serviceProvider.GetService<BlobServiceClient>();
            if (blobServiceClient == null) return;

            var logger = serviceProvider.GetRequiredService<ILogger<BlobServiceClient>>();
            var containerName = environment.IsProduction() ? "production" : "development";

            await blobServiceClient.GetBlobContainerClient(containerName).CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            await blobServiceClient.GetBlobContainerClient("seeds").CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            logger.LogInformation("Ensured blob containers '{ContainerName}' and 'seeds' exist", containerName);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<BlobServiceClient>>();
            logger.LogWarning(ex, "Failed to ensure Blob Storage containers exist on startup.");
        }
    }
}