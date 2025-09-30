using Microsoft.Extensions.Logging;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Service interface for seeding population data from Excel files
/// </summary>
public interface IPopulationSeederService
{
    /// <summary>
    /// Seeds population data from the default Excel file location if it exists
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger instance</param>
    Task SeedAsync(ApplicationDbContext context, ILogger logger);

    /// <summary>
    /// Seeds population data from a specific Excel file path
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="excelFilePath">Path to the Excel file</param>
    /// <param name="logger">Logger instance</param>
    Task SeedPopulationDataAsync(ApplicationDbContext context, string excelFilePath, ILogger logger);
}