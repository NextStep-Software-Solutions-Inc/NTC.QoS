using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed;

namespace NTC.QoS.Infrastructure.Services;

public class DatabaseSeedingService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeedingService> _logger;

    public DatabaseSeedingService(IServiceProvider serviceProvider, ILogger<DatabaseSeedingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting database seeding service...");
            
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            
            // Run the seeding
            await LocationSeeder.SeedAllAsync(dbContext, _logger);
            
            _logger.LogInformation("Database seeding service completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw; // Re-throw to prevent the application from starting with incomplete data
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}