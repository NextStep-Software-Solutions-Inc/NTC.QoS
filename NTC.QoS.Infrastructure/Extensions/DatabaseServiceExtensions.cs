using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for database service registration
/// </summary>
internal static class DatabaseServiceExtensions
{
    /// <summary>
    /// Registers database-related services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure database options with defaults
        services.Configure<DatabaseOptions>(databaseOptions =>
        {
            var section = configuration.GetSection(DatabaseOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(databaseOptions);
            }
            // Defaults are already set in DatabaseOptions class
        });
        
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSqlConnection string is not configured. " +
                "For development, use: dotnet user-secrets set \"ConnectionStrings:PostgreSqlConnection\" \"your_connection_string\" " +
                "For production, set the ConnectionStrings__PostgreSqlConnection environment variable.");
        }

        // Register interceptors
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // Register DbContext with interceptors
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var audit = sp.GetRequiredService<AuditInterceptor>();
            var softDelete = sp.GetRequiredService<SoftDeleteInterceptor>();
            
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseNetTopologySuite();
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: databaseOptions.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                        errorCodesToAdd: null);
                })
                .AddInterceptors(audit, softDelete);
        });

        return services;
    }
}