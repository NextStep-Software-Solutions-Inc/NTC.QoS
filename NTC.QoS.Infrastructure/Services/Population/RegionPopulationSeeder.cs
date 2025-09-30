using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Data.Helpers;
using ClosedXML.Excel;

namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Service for seeding population data for regions from Excel files
/// </summary>
public class RegionPopulationSeeder
{
    /// <summary>
    /// Seeds region population data from Excel worksheet
    /// Expected columns: RegionName (A), TotalPopulation (B), HouseholdPopulation (C), NumberOfHouseholds (D)
    /// </summary>
    /// <param name="workbook">Excel workbook containing the data</param>
    /// <param name="existingRegions">List of existing regions in the database</param>
    /// <param name="stats">Statistics tracking object</param>
    /// <param name="logger">Logger instance</param>
    public static Task SeedFromExcelAsync(XLWorkbook workbook, List<Region> existingRegions, PopulationSeedingStats stats, ILogger logger)
    {
        try
        {
            var worksheet = workbook.Worksheet("Regions");
            if (worksheet == null)
            {
                logger.LogWarning("'Regions' sheet not found in workbook");
                return Task.CompletedTask;
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null)
            {
                logger.LogWarning("'Regions' sheet is empty");
                return Task.CompletedTask;
            }

            var rowCount = lastRow.RowNumber();
            logger.LogInformation("Processing {Count} regions from Excel sheet", rowCount - 1);

            for (int row = 2; row <= rowCount; row++) // Skip header row
            {
                try
                {
                    var regionName = worksheet.Cell(row, 1).GetString().Trim();
                    if (string.IsNullOrEmpty(regionName)) continue;

                    // Parse population data with validation
                    logger.LogTrace("Processing region '{RegionName}' at row {Row}", regionName, row);
                    logger.LogTrace("Raw cell values - TotalPop: '{TotalPopRaw}' ({TotalPopType}), HouseholdPop: '{HouseholdPopRaw}' ({HouseholdPopType}), HouseholdCount: '{HouseholdCountRaw}' ({HouseholdCountType})", 
                        worksheet.Cell(row, 2).Value, worksheet.Cell(row, 2).Value.GetType().Name,
                        worksheet.Cell(row, 3).Value, worksheet.Cell(row, 3).Value.GetType().Name, 
                        worksheet.Cell(row, 4).Value, worksheet.Cell(row, 4).Value.GetType().Name);

                    var totalPopulation = PopulationDataParser.ParsePopulationValue(worksheet.Cell(row, 2).Value, "TotalPopulation", regionName, row, logger);
                    var householdPopulation = PopulationDataParser.ParsePopulationValue(worksheet.Cell(row, 3).Value, "HouseholdPopulation", regionName, row, logger);
                    var householdCount = PopulationDataParser.ParseHouseholdValue(worksheet.Cell(row, 4).Value, "NumberOfHouseholds", regionName, row, logger);

                    // Find matching region using fuzzy matching
                    var matchedRegion = NameMatcher.FindBestMatch(regionName, existingRegions, r => r.Name);

                    if (matchedRegion != null)
                    {
                        // Update population data
                        matchedRegion.TotalPopulation = totalPopulation;
                        matchedRegion.HouseholdPopulation = householdPopulation;
                        matchedRegion.HouseholdCount = householdCount;

                        logger.LogDebug("Region matched: '{ExistingName}' ? '{ExcelName}' (Pop: {TotalPop:N0}, HH: {HouseholdCount:N0})", 
                            matchedRegion.Name, regionName, totalPopulation, householdCount);
                        stats.RegionsMatched++;
                    }
                    else
                    {
                        logger.LogWarning("Region not found in database: '{RegionName}' (Row {Row})", regionName, row);
                        stats.RegionsNotFound++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing region at row {Row}", row);
                    stats.RegionsErrors++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Regions sheet");
            throw;
        }

        return Task.CompletedTask;
    }
}