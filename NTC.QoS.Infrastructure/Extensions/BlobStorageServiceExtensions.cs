using Azure.Storage.Blobs;
using Azure.Storage;
using System.Data.Common;
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

        services.AddSingleton(new BlobServiceClient(blobConnectionString));

        var connectionStringBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = blobConnectionString
        };
        if (connectionStringBuilder.TryGetValue("AccountName", out var accountName) &&
            connectionStringBuilder.TryGetValue("AccountKey", out var accountKey) &&
            accountName is string accountNameValue &&
            accountKey is string accountKeyValue)
        {
            services.AddSingleton(new StorageSharedKeyCredential(accountNameValue, accountKeyValue));
        }

        services.AddScoped<IBlobStorageService, BlobStorageService>();

        return services;
    }
}
