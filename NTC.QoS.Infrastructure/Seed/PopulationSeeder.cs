using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Services.Population;

namespace NTC.QoS.Infrastructure.Seed;

/// <summary>
/// Static seeder for population data from Excel files with multiple sheets using ClosedXML
/// Follows the same pattern as LocationSeeder - simple utility class for one-time seeding
/// Handles hierarchical relationships between regions and municipalities
/// 
/// This class now uses dependency injection to resolve PopulationSeederService for improved maintainability and testability
/// </summary>
public static class PopulationSeeder
{
    /// <summary>
    /// Seeds population data from the default Excel file location if it exists
    /// Default file location: census_population_data.xlsx in the current directory
    /// Expected sheets:
    /// - "Regions" sheet: RegionName (A), TotalPopulation (B), HouseholdPopulation (C), NumberOfHouseholds (D)
    /// - "Provinces" sheet: RegionName (A), ProvinceName (B), TotalPopulation (C), HouseholdPopulation (D), NumberOfHouseholds (E)
    /// - "Municipalities" sheet: RegionName (A), ProvinceName (B), MunicipalityName (C), TotalPopulation (D), HouseholdPopulation (E), NumberOfHouseholds (F)
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger instance</param>
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        using var serviceProvider = CreateServiceProvider();
        var seederService = serviceProvider.GetRequiredService<IPopulationSeederService>();
        await seederService.SeedAsync(context, logger);
    }

    /// <summary>
    /// Creates a minimal service provider for resolving the population seeder service
    /// This is used when the static methods need to work outside of the DI container
    /// </summary>
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPopulationSeederService, PopulationSeederService>();
        return services.BuildServiceProvider();
    }
}