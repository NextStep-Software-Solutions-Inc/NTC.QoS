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

        services.AddSingleton(provider => new BlobServiceClient(blobConnectionString));
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        return services;
    }
}