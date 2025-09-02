using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NTC.QoS.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        // Try different base paths to find configuration files
        var basePaths = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "NTC.QoS.API"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "NTC.QoS.API"),
            Path.GetDirectoryName(typeof(ApplicationDbContextFactory).Assembly.Location) ?? Directory.GetCurrentDirectory()
        };

        ConfigurationBuilder configBuilder = new();
        
        foreach (var basePath in basePaths)
        {
            if (Directory.Exists(basePath))
            {
                configBuilder.SetBasePath(basePath);
                break;
            }
        }

        var configuration = configBuilder
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddUserSecrets<ApplicationDbContextFactory>() // 🔑 This enables reading from secrets.json
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Create detailed error message showing what was checked
            var checkedPaths = string.Join(", ", basePaths.Where(Directory.Exists));
            throw new InvalidOperationException(
                "PostgreSqlConnection not found in configuration. " +
                "Checked sources: appsettings.json, appsettings.{env}.json, user secrets, environment variables. " +
                $"Current environment: {env}, " +
                $"Checked base paths: {checkedPaths}, " +
                $"Current directory: {Directory.GetCurrentDirectory()}");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, x => x.UseNetTopologySuite());

        return new ApplicationDbContext(optionsBuilder.Options, configuration);
    }
}
