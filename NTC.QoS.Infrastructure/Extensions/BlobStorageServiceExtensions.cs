using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        // Configure options
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));

        // Register BlobServiceClient
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            
            if (options.UseDevelopmentStorage)
            {
                // Use Azurite emulator connection string
                return new BlobServiceClient("UseDevelopmentStorage=true");
            }
            
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured. Please set AzureBlobStorage:ConnectionString in configuration.");
            }

            return new BlobServiceClient(options.ConnectionString);
        });

        // Register the blob storage service
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        return services;
    }

    /// <summary>
    /// Validates the blob storage configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ValidateBlobStorageConfiguration(this IServiceCollection services)
    {
        services.AddOptions<BlobStorageOptions>()
            .Validate(options => !string.IsNullOrEmpty(options.ConnectionString) || options.UseDevelopmentStorage,
                "Azure Blob Storage connection string must be provided unless using development storage")
            .Validate(options => options.MaxFileSizeMB > 0,
                "Maximum file size must be greater than 0")
            .Validate(options => options.AllowedContentTypes.Length > 0,
                "At least one allowed content type must be specified")
            .Validate(options => !string.IsNullOrEmpty(options.DefaultContainer),
                "Default container name must be specified");

        return services;
    }
}