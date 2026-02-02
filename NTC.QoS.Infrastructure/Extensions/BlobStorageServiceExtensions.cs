using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Azure Blob Storage services
/// </summary>
public static class BlobStorageServiceExtensions
{
    /// <summary>
    /// Registers Azure Blob Storage services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBlobStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        var blobConnectionString = configuration.GetValue<string>("ConnectionStrings:BlobStorageConnection") ?? throw new InvalidOperationException("Blob Storage connection string is not configured in ConnectionStrings:BlobStorageConnection");

        // register Azure blob storage DI
        services.AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(BlobStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure BlobClientOptions with extended timeouts for large file uploads
        var blobClientOptions = new Azure.Storage.Blobs.BlobClientOptions
        {
            Retry =
            {
                MaxRetries = 10, // Increased from default 3 to 10
                NetworkTimeout = TimeSpan.FromMinutes(10), // Increased from 100 seconds to 10 minutes for large files
                Delay = TimeSpan.FromSeconds(2), // Initial delay between retries
                MaxDelay = TimeSpan.FromSeconds(30), // Max delay between retries
                Mode = Azure.Core.RetryMode.Exponential
            }
        };

        services.AddSingleton(provider => new BlobServiceClient(blobConnectionString, blobClientOptions));
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        return services;
    }
}