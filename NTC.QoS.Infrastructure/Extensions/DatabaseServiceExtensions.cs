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

        // Enhance connection string with pool settings if not already present
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        
        // Set connection pool settings for better resilience
        if (!builder.ContainsKey("Minimum Pool Size"))
            builder.MinPoolSize = 5; // Keep connections warm
        
        if (!builder.ContainsKey("Maximum Pool Size"))
            builder.MaxPoolSize = 50; // Allow more concurrent connections
        
        if (!builder.ContainsKey("Connection Idle Lifetime"))
            builder.ConnectionIdleLifetime = 300; // 5 minutes - prevents stale connections
        
        if (!builder.ContainsKey("Timeout"))
            builder.Timeout = 60; // 60 seconds connection timeout
        
        if (!builder.ContainsKey("Command Timeout"))
            builder.CommandTimeout = 600; // 10 minutes for long-running commands
        
        connectionString = builder.ConnectionString;

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
                    // Increase command timeout for long-running spatial queries
                    // Default is 30 seconds, which can be too short for complex geometry operations
                    npgsql.CommandTimeout(300); // 5 minutes
                })
                .AddInterceptors(audit, softDelete);
        });

        return services;
    }
}