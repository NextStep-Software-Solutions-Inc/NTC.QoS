using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Data.Helpers;
using ClosedXML.Excel;

namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Service for seeding population data for municipalities from Excel files
/// </summary>
public class MunicipalityPopulationSeeder
{
    /// <summary>
    /// Seeds municipality population data from Excel worksheet
    /// Expected columns: RegionName (A), ProvinceName (B), MunicipalityName (C), TotalPopulation (D), HouseholdPopulation (E), NumberOfHouseholds (F)
    /// </summary>
    /// <param name="workbook">Excel workbook containing the data</param>
    /// <param name="existingMunicipalities">List of existing municipalities in the database</param>
    /// <param name="existingRegions">List of existing regions in the database</param>
    /// <param name="stats">Statistics tracking object</param>
    /// <param name="logger">Logger instance</param>
    public static Task SeedFromExcelAsync(XLWorkbook workbook, List<Municipality> existingMunicipalities, List<Region> existingRegions, PopulationSeedingStats stats, ILogger logger)
    {
        try
        {
            var worksheet = workbook.Worksheet("Municipalities");
            if (worksheet == null)
            {
                logger.LogWarning("'Municipalities' sheet not found in workbook");
                return Task.CompletedTask;
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null)
            {
                logger.LogWarning("'Municipalities' sheet is empty");
                return Task.CompletedTask;
            }

            var rowCount = lastRow.RowNumber();
            logger.LogInformation("Processing {Count} municipalities from Excel sheet", rowCount - 1);

            // Log header structure for verification
            var headerA = worksheet.Cell(1, 1).GetString();
            var headerB = worksheet.Cell(1, 2).GetString();
            var headerC = worksheet.Cell(1, 3).GetString();
            var headerD = worksheet.Cell(1, 4).GetString();
            var headerE = worksheet.Cell(1, 5).GetString();
            var headerF = worksheet.Cell(1, 6).GetString();
            logger.LogInformation("Municipality sheet headers: A='{HeaderA}', B='{HeaderB}', C='{HeaderC}', D='{HeaderD}', E='{HeaderE}', F='{HeaderF}'", 
                headerA, headerB, headerC, headerD, headerE, headerF);

            for (int row = 2; row <= rowCount; row++) // Skip header row
            {
                try
                {
                    var regionName = worksheet.Cell(row, 1).GetString().Trim();        // Column A: RegionName
                    var provinceName = worksheet.Cell(row, 2).GetString().Trim();     // Column B: ProvinceName  
                    var municipalityName = worksheet.Cell(row, 3).GetString().Trim(); // Column C: MunicipalityName
                    
                    if (string.IsNullOrEmpty(regionName) || string.IsNullOrEmpty(municipalityName)) continue;

                    // Parse population data with validation from correct columns
                    logger.LogTrace("Processing municipality '{MunicipalityName}' in province '{ProvinceName}', region '{RegionName}' at row {Row}", 
                        municipalityName, provinceName, regionName, row);
                    
                    // Log raw cell values for debugging (columns D, E, F)
                    logger.LogTrace("Raw cell values - TotalPop: '{TotalPopRaw}' ({TotalPopType}), HouseholdPop: '{HouseholdPopRaw}' ({HouseholdPopType}), HouseholdCount: '{HouseholdCountRaw}' ({HouseholdCountType})", 
                        worksheet.Cell(row, 4).Value, worksheet.Cell(row, 4).Value.GetType().Name,
                        worksheet.Cell(row, 5).Value, worksheet.Cell(row, 5).Value.GetType().Name, 
                        worksheet.Cell(row, 6).Value, worksheet.Cell(row, 6).Value.GetType().Name);

                    // Parse population data from correct columns
                    var totalPopulation = PopulationDataParser.ParsePopulationValue(worksheet.Cell(row, 4).Value, "TotalPopulation", municipalityName, row, logger);         // Column D
                    var householdPopulation = PopulationDataParser.ParsePopulationValue(worksheet.Cell(row, 5).Value, "HouseholdPopulation", municipalityName, row, logger); // Column E
                    var householdCount = PopulationDataParser.ParseHouseholdValue(worksheet.Cell(row, 6).Value, "NumberOfHouseholds", municipalityName, row, logger);         // Column F

                    logger.LogTrace("Parsed values for '{MunicipalityName}': TotalPop={TotalPop}, HouseholdPop={HouseholdPop}, HouseholdCount={HouseholdCount}", 
                        municipalityName, totalPopulation, householdPopulation, householdCount);

                    // Find matching region first
                    var matchedRegion = NameMatcher.FindBestMatch(regionName, existingRegions, r => r.Name);
                    if (matchedRegion == null)
                    {
                        logger.LogWarning("Region '{RegionName}' not found for municipality '{MunicipalityName}' (Row {Row})", 
                            regionName, municipalityName, row);
                        stats.MunicipalitiesRegionNotFound++;
                        continue;
                    }

                    logger.LogTrace("Region matched: '{RegionName}' -> '{MatchedRegionName}' (ID: {RegionId})", 
                        regionName, matchedRegion.Name, matchedRegion.Id);

                    // Find municipalities within the matched region's provinces
                    var municipalitiesInRegion = existingMunicipalities
                        .Where(m => m.Province?.RegionId == matchedRegion.Id)
                        .ToList();

                    logger.LogTrace("Found {Count} municipalities in region '{RegionName}'", 
                        municipalitiesInRegion.Count, matchedRegion.Name);

                    // Find matching municipality within the region context
                    var matchedMunicipality = NameMatcher.FindBestMatch(municipalityName, municipalitiesInRegion, m => m.Name);

                    if (matchedMunicipality != null)
                    {
                        // Log before values for comparison
                        logger.LogTrace("Before update - '{ExistingName}': TotalPop={OldTotalPop}, HouseholdPop={OldHouseholdPop}, HouseholdCount={OldHouseholdCount}", 
                            matchedMunicipality.Name, matchedMunicipality.TotalPopulation, matchedMunicipality.HouseholdPopulation, matchedMunicipality.HouseholdCount);

                        // Update population data
                        matchedMunicipality.TotalPopulation = totalPopulation;
                        matchedMunicipality.HouseholdPopulation = householdPopulation;
                        matchedMunicipality.HouseholdCount = householdCount;

                        logger.LogDebug("Municipality matched: '{ExistingName}' ? '{ExcelName}' in region '{RegionName}' (Pop: {TotalPop:N0}, HH: {HouseholdCount:N0})", 
                            matchedMunicipality.Name, municipalityName, matchedRegion.Name, totalPopulation, householdCount);
                        stats.MunicipalitiesMatched++;
                    }
                    else
                    {
                        // Log detailed info about failed matches for debugging
                        var availableNames = municipalitiesInRegion.Take(10).Select(m => m.Name).ToList();
                        if (municipalitiesInRegion.Count > 10)
                        {
                            availableNames.Add($"... and {municipalitiesInRegion.Count - 10} more");
                        }
                        
                        logger.LogWarning("Municipality '{MunicipalityName}' not found in region '{RegionName}' (Row {Row}). Available municipalities in this region (showing first 10): [{AvailableNames}]", 
                            municipalityName, matchedRegion.Name, row, string.Join(", ", availableNames));
                        stats.MunicipalitiesNotFound++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing municipality at row {Row}: {Message}", row, ex.Message);
                    stats.MunicipalitiesErrors++;
                }

                // Log progress every 50 municipalities
                if ((row - 1) % 50 == 0)
                {
                    logger.LogInformation("Municipalities progress: {Processed}/{Total} ({Matched} matched, {NotFound} not found, {RegionNotFound} region not found, {Errors} errors)", 
                        row - 1, rowCount - 1, stats.MunicipalitiesMatched, stats.MunicipalitiesNotFound, stats.MunicipalitiesRegionNotFound, stats.MunicipalitiesErrors);
                }
            }

            logger.LogInformation("Municipalities processing summary: {Processed} processed, {Matched} matched, {NotFound} not found, {RegionNotFound} region not found, {Errors} errors", 
                rowCount - 1, stats.MunicipalitiesMatched, stats.MunicipalitiesNotFound, stats.MunicipalitiesRegionNotFound, stats.MunicipalitiesErrors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Municipalities sheet: {Message}", ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }
}