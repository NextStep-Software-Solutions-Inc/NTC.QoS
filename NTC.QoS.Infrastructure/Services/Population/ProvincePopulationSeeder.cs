using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Data.Helpers;
using ClosedXML.Excel;

namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Service for seeding population data for provinces from Excel files
/// </summary>
public class ProvincePopulationSeeder
{
    /// <summary>
    /// Seeds province population data from Excel worksheet
    /// Expected columns: RegionName (A), ProvinceName (B), TotalPopulation (C), HouseholdPopulation (D), NumberOfHouseholds (E)
    /// </summary>
    /// <param name="workbook">Excel workbook containing the data</param>
    /// <param name="existingProvinces">List of existing provinces in the database</param>
    /// <param name="existingRegions">List of existing regions in the database</param>
    /// <param name="stats">Statistics tracking object</param>
    /// <param name="logger">Logger instance</param>
    public static Task SeedFromExcelAsync(XLWorkbook workbook, List<Province> existingProvinces, List<Region> existingRegions, PopulationSeedingStats stats, ILogger logger)
    {
        try
        {
            var worksheet = workbook.Worksheet("Provinces");
            if (worksheet == null)
            {
                logger.LogWarning("'Provinces' sheet not found in workbook");
                return Task.CompletedTask;
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null)
            {
                logger.LogWarning("'Provinces' sheet is empty");
                return Task.CompletedTask;
            }

            var rowCount = lastRow.RowNumber();
            logger.LogInformation("Processing {Count} provinces from Excel sheet", rowCount - 1);

            for (int row = 2; row <= rowCount; row++) // Skip header row
            {
                try
                {
                    var regionName = worksheet.Cell(row, 1).GetString().Trim();
                    var provinceName = worksheet.Cell(row, 2).GetString().Trim();
                    
                    if (string.IsNullOrEmpty(regionName) || string.IsNullOrEmpty(provinceName)) continue;

                    // Parse population data with validation
                    logger.LogTrace("Processing province '{ProvinceName}' in region '{RegionName}' at row {Row}", provinceName, regionName, row);
                    logger.LogTrace("Raw cell values - TotalPop: '{TotalPopRaw}' ({TotalPopType}), HouseholdPop: '{HouseholdPopRaw}' ({HouseholdPopType}), HouseholdCount: '{HouseholdCountRaw}' ({HouseholdCountType})", 
                        worksheet.Cell(row, 3).Value, worksheet.Cell(row, 3).Value.GetType().Name,
                        worksheet.Cell(row, 4).Value, worksheet.Cell(row, 4).Value.GetType().Name, 
                        worksheet.Cell(row, 5).Value, worksheet.Cell(row, 5).Value.GetType().Name);

                    var totalPopulation = PopulationDataParser.ParsePopulationValue(worksheet.Cell(row, 3).Value, "TotalPopulation", provinceName, row, logger);
                    var householdPopulation = PopulationDataParser.ParsePopulationValue(worksheet.Cell(row, 4).Value, "HouseholdPopulation", provinceName, row, logger);
                    var householdCount = PopulationDataParser.ParseHouseholdValue(worksheet.Cell(row, 5).Value, "NumberOfHouseholds", provinceName, row, logger);

                    // Find matching region first
                    var matchedRegion = NameMatcher.FindBestMatch(regionName, existingRegions, r => r.Name);
                    if (matchedRegion == null)
                    {
                        logger.LogWarning("Region '{RegionName}' not found for province '{ProvinceName}' (Row {Row})", 
                            regionName, provinceName, row);
                        stats.ProvincesRegionNotFound++;
                        continue;
                    }

                    // Find provinces within the matched region
                    var provincesInRegion = existingProvinces
                        .Where(p => p.RegionId == matchedRegion.Id)
                        .ToList();

                    // Find matching province within the region context
                    var matchedProvince = NameMatcher.FindBestMatch(provinceName, provincesInRegion, p => p.Name);

                    if (matchedProvince != null)
                    {
                        // Update population data
                        matchedProvince.TotalPopulation = totalPopulation;
                        matchedProvince.HouseholdPopulation = householdPopulation;
                        matchedProvince.HouseholdCount = householdCount;

                        logger.LogDebug("Province matched: '{ExistingName}' ? '{ExcelName}' in region '{RegionName}' (Pop: {TotalPop:N0}, HH: {HouseholdCount:N0})", 
                            matchedProvince.Name, provinceName, matchedRegion.Name, totalPopulation, householdCount);
                        stats.ProvincesMatched++;
                    }
                    else
                    {
                        // Log detailed info about failed matches for debugging
                        var availableNames = provincesInRegion.Select(p => p.Name).ToList();
                        logger.LogWarning("Province '{ProvinceName}' not found in region '{RegionName}' (Row {Row}). Available provinces in this region: [{AvailableNames}]", 
                            provinceName, matchedRegion.Name, row, string.Join(", ", availableNames));
                        stats.ProvincesNotFound++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing province at row {Row}", row);
                    stats.ProvincesErrors++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Provinces sheet");
            throw;
        }

        return Task.CompletedTask;
    }
}