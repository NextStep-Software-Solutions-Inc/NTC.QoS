using Microsoft.Extensions.Logging;

namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Utility class for logging population seeding results and statistics
/// </summary>
public static class PopulationSeedingLogger
{
    /// <summary>
    /// Logs comprehensive seeding results with statistics and success rates
    /// </summary>
    /// <param name="stats">The seeding statistics to log</param>
    /// <param name="logger">Logger instance</param>
    public static void LogSeedingResults(PopulationSeedingStats stats, ILogger logger)
    {
        logger.LogInformation("=== Population Data Seeding Results ===");

        if (stats.HasRegionData)
        {
            logger.LogInformation("Regions - Matched: {Matched}, Not Found: {NotFound}, Errors: {Errors}", 
                stats.RegionsMatched, stats.RegionsNotFound, stats.RegionsErrors);
        }

        if (stats.HasProvinceData)
        {
            logger.LogInformation("Provinces - Matched: {Matched}, Not Found: {NotFound}, Region Not Found: {RegionNotFound}, Errors: {Errors}", 
                stats.ProvincesMatched, stats.ProvincesNotFound, stats.ProvincesRegionNotFound, stats.ProvincesErrors);
        }

        if (stats.HasMunicipalityData)
        {
            logger.LogInformation("Municipalities - Matched: {Matched}, Not Found: {NotFound}, Region Not Found: {RegionNotFound}, Errors: {Errors}", 
                stats.MunicipalitiesMatched, stats.MunicipalitiesNotFound, stats.MunicipalitiesRegionNotFound, stats.MunicipalitiesErrors);
        }

        var totalMatched = stats.RegionsMatched + stats.ProvincesMatched + stats.MunicipalitiesMatched;
        var totalNotFound = stats.RegionsNotFound + stats.ProvincesNotFound + stats.MunicipalitiesNotFound + 
                           stats.ProvincesRegionNotFound + stats.MunicipalitiesRegionNotFound;
        var totalErrors = stats.RegionsErrors + stats.ProvincesErrors + stats.MunicipalitiesErrors;

        logger.LogInformation("Summary - Total Matched: {TotalMatched}, Total Not Found: {TotalNotFound}, Total Errors: {TotalErrors}", 
            totalMatched, totalNotFound, totalErrors);
        
        if (totalMatched > 0)
        {
            var successRate = Math.Round((double)totalMatched / (totalMatched + totalNotFound + totalErrors) * 100, 2);
            logger.LogInformation("Success Rate: {SuccessRate}%", successRate);
        }

        logger.LogInformation("Population data seeding completed successfully!");
    }
}