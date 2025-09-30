using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;
using ClosedXML.Excel;

namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Main service for coordinating population data seeding from Excel files
/// Orchestrates the seeding process across regions, provinces, and municipalities
/// </summary>
public class PopulationSeederService : IPopulationSeederService
{
    /// <summary>
    /// Seeds population data from the default Excel file location if it exists
    /// Default file location: census_population_data.xlsx in the current directory
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger instance</param>
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check for population data file in the same location as GeoJSON files
        var populationFilePath = Path.Combine(Environment.CurrentDirectory, "census_population_data.xlsx");
        
        if (System.IO.File.Exists(populationFilePath))
        {
            await SeedPopulationDataAsync(context, populationFilePath, logger);
        }
        else
        {
            logger.LogInformation("Population data file not found at: {FilePath}. Skipping population seeding.", populationFilePath);
            logger.LogInformation("To seed population data, place your Excel file at: {FilePath}", populationFilePath);
        }
    }

    /// <summary>
    /// Seeds population data from Excel file with multiple sheets
    /// Expected sheets:
    /// - "Regions" sheet: RegionName (A), TotalPopulation (B), HouseholdPopulation (C), NumberOfHouseholds (D)
    /// - "Provinces" sheet: RegionName (A), ProvinceName (B), TotalPopulation (C), HouseholdPopulation (D), NumberOfHouseholds (E)
    /// - "Municipalities" sheet: RegionName (A), ProvinceName (B), MunicipalityName (C), TotalPopulation (D), HouseholdPopulation (E), NumberOfHouseholds (F)
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="excelFilePath">Path to the Excel file</param>
    /// <param name="logger">Logger instance</param>
    public async Task SeedPopulationDataAsync(ApplicationDbContext context, string excelFilePath, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting population data seeding from Excel file: {FilePath}", excelFilePath);

            if (!System.IO.File.Exists(excelFilePath))
            {
                logger.LogError("Excel file not found: {FilePath}", excelFilePath);
                throw new FileNotFoundException($"Excel file not found: {excelFilePath}");
            }

            using var workbook = new XLWorkbook(excelFilePath);
            
            var stats = new PopulationSeedingStats();

            // Check which sheets are available
            var availableSheets = workbook.Worksheets.Select(w => w.Name).ToList();
            logger.LogInformation("Available sheets: {Sheets}", string.Join(", ", availableSheets));

            // Load existing data for matching
            var existingRegions = await LoadExistingRegionsAsync(context);
            var existingProvinces = await LoadExistingProvincesAsync(context);
            var existingMunicipalities = await LoadExistingMunicipalitiesAsync(context);

            // Seed regions first (parent entities)
            if (availableSheets.Contains("Regions"))
            {
                await RegionPopulationSeeder.SeedFromExcelAsync(workbook, existingRegions, stats, logger);
            }
            else
            {
                logger.LogWarning("'Regions' sheet not found - skipping region seeding");
            }

            // Then seed provinces (child of regions)
            if (availableSheets.Contains("Provinces"))
            {
                await ProvincePopulationSeeder.SeedFromExcelAsync(workbook, existingProvinces, existingRegions, stats, logger);
            }
            else
            {
                logger.LogWarning("'Provinces' sheet not found - skipping province seeding");
            }

            // Finally seed municipalities (child of provinces)
            if (availableSheets.Contains("Municipalities"))
            {
                await MunicipalityPopulationSeeder.SeedFromExcelAsync(workbook, existingMunicipalities, existingRegions, stats, logger);
            }
            else
            {
                logger.LogWarning("'Municipalities' sheet not found - skipping municipality seeding");
            }

            // Save all changes
            var changesSaved = await context.SaveChangesAsync();
            logger.LogInformation("Saved {ChangeCount} changes to database", changesSaved);

            // Log final statistics
            PopulationSeedingLogger.LogSeedingResults(stats, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during population data seeding from Excel file: {FilePath}", excelFilePath);
            throw;
        }
    }

    /// <summary>
    /// Loads existing regions from the database
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>List of existing regions</returns>
    private static async Task<List<Region>> LoadExistingRegionsAsync(ApplicationDbContext context)
    {
        return await context.Regions.ToListAsync();
    }

    /// <summary>
    /// Loads existing provinces with their regions from the database
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>List of existing provinces with regions included</returns>
    private static async Task<List<Province>> LoadExistingProvincesAsync(ApplicationDbContext context)
    {
        return await context.Provinces
            .Include(p => p.Region)
            .ToListAsync();
    }

    /// <summary>
    /// Loads existing municipalities with their provinces and regions from the database
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>List of existing municipalities with full hierarchy included</returns>
    private static async Task<List<Municipality>> LoadExistingMunicipalitiesAsync(ApplicationDbContext context)
    {
        return await context.Municipalities
            .Include(m => m.Province)
            .ThenInclude(p => p.Region)
            .ToListAsync();
    }
}