using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Data.Helpers;
using NTC.QoS.Infrastructure.Persistence;
using ClosedXML.Excel;

namespace NTC.QoS.Infrastructure.Seed;

/// <summary>
/// Static seeder for population data from Excel files with multiple sheets using ClosedXML
/// Follows the same pattern as LocationSeeder - simple utility class for one-time seeding
/// Handles hierarchical relationships between regions and municipalities
/// </summary>
public static class PopulationSeeder
{
    /// <summary>
    /// Seeds population data from the default Excel file location if it exists
    /// Default file location: census_population_data.xlsx in the current directory
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
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
    /// - "Municipalities" sheet: RegionName (A), MunicipalityName (B), TotalPopulation (C), HouseholdPopulation (D), NumberOfHouseholds (E)
    /// </summary>
    public static async Task SeedPopulationDataAsync(ApplicationDbContext context, string excelFilePath, ILogger logger)
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
            var existingRegions = await context.Regions.ToListAsync();
            var existingProvinces = await context.Provinces
                .Include(p => p.Region)
                .ToListAsync();
            var existingMunicipalities = await context.Municipalities
                .Include(m => m.Province)
                .ThenInclude(p => p.Region)
                .ToListAsync();

            // Seed regions first (parent entities)
            if (availableSheets.Contains("Regions"))
            {
                await SeedRegionsFromExcelAsync(workbook, context, existingRegions, stats, logger);
            }
            else
            {
                logger.LogWarning("'Regions' sheet not found - skipping region seeding");
            }

            // Then seed provinces (child of regions)
            if (availableSheets.Contains("Provinces"))
            {
                await SeedProvincesFromExcelAsync(workbook, context, existingProvinces, existingRegions, stats, logger);
            }
            else
            {
                logger.LogWarning("'Provinces' sheet not found - skipping province seeding");
            }

            // Finally seed municipalities (child of provinces)
            if (availableSheets.Contains("Municipalities"))
            {
                await SeedMunicipalitiesFromExcelAsync(workbook, context, existingMunicipalities, existingRegions, stats, logger);
            }
            else
            {
                logger.LogWarning("'Municipalities' sheet not found - skipping municipality seeding");
            }

            // Save all changes
            var changesSaved = await context.SaveChangesAsync();
            logger.LogInformation("Saved {ChangeCount} changes to database", changesSaved);

            // Log final statistics
            LogSeedingResults(stats, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during population data seeding from Excel file: {FilePath}", excelFilePath);
            throw;
        }
    }

    private static Task SeedRegionsFromExcelAsync(XLWorkbook workbook, ApplicationDbContext context, List<Region> existingRegions, PopulationSeedingStats stats, ILogger logger)
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

                    var totalPopulation = ParsePopulationValue(worksheet.Cell(row, 2).Value, "TotalPopulation", regionName, row, logger);
                    var householdPopulation = ParsePopulationValue(worksheet.Cell(row, 3).Value, "HouseholdPopulation", regionName, row, logger);
                    var householdCount = ParseHouseholdValue(worksheet.Cell(row, 4).Value, "NumberOfHouseholds", regionName, row, logger);

                    // Find matching region using fuzzy matching
                    var matchedRegion = NameMatcher.FindBestMatch(regionName, existingRegions, r => r.Name);

                    if (matchedRegion != null)
                    {
                        // Update population data
                        matchedRegion.TotalPopulation = totalPopulation;
                        matchedRegion.HouseholdPopulation = householdPopulation;
                        matchedRegion.HouseholdCount = householdCount;

                        logger.LogDebug("Region matched: '{ExistingName}' ← '{ExcelName}' (Pop: {TotalPop:N0}, HH: {HouseholdCount:N0})", 
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

    private static Task SeedProvincesFromExcelAsync(XLWorkbook workbook, ApplicationDbContext context, List<Province> existingProvinces, List<Region> existingRegions, PopulationSeedingStats stats, ILogger logger)
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

                    var totalPopulation = ParsePopulationValue(worksheet.Cell(row, 3).Value, "TotalPopulation", provinceName, row, logger);
                    var householdPopulation = ParsePopulationValue(worksheet.Cell(row, 4).Value, "HouseholdPopulation", provinceName, row, logger);
                    var householdCount = ParseHouseholdValue(worksheet.Cell(row, 5).Value, "NumberOfHouseholds", provinceName, row, logger);

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

                        logger.LogDebug("Province matched: '{ExistingName}' ← '{ExcelName}' in region '{RegionName}' (Pop: {TotalPop:N0}, HH: {HouseholdCount:N0})", 
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

    private static Task SeedMunicipalitiesFromExcelAsync(XLWorkbook workbook, ApplicationDbContext context, List<Municipality> existingMunicipalities, List<Region> existingRegions, PopulationSeedingStats stats, ILogger logger)
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
                    var totalPopulation = ParsePopulationValue(worksheet.Cell(row, 4).Value, "TotalPopulation", municipalityName, row, logger);         // Column D
                    var householdPopulation = ParsePopulationValue(worksheet.Cell(row, 5).Value, "HouseholdPopulation", municipalityName, row, logger); // Column E
                    var householdCount = ParseHouseholdValue(worksheet.Cell(row, 6).Value, "NumberOfHouseholds", municipalityName, row, logger);         // Column F

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

                        logger.LogDebug("Municipality matched: '{ExistingName}' ← '{ExcelName}' in region '{RegionName}' (Pop: {TotalPop:N0}, HH: {HouseholdCount:N0})", 
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

    private static int? ParsePopulationValue(object? cellValue, string fieldName, string locationName, int row, ILogger logger)
    {
        if (cellValue == null)
            return null;

        // Convert cell value to string first for better debugging
        var cellString = cellValue.ToString()?.Trim();
        if (string.IsNullOrEmpty(cellString))
            return null;

        try
        {
            // Handle different cell value types from ClosedXML
            int value;
            
            switch (cellValue)
            {
                case int intValue:
                    value = intValue;
                    break;
                case double doubleValue:
                    // Excel often stores numbers as doubles
                    value = (int)Math.Round(doubleValue);
                    break;
                case decimal decimalValue:
                    value = (int)Math.Round(decimalValue);
                    break;
                case string stringValue:
                    // Remove common formatting (commas, spaces)
                    var cleanString = stringValue.Replace(",", "").Replace(" ", "").Trim();
                    if (!int.TryParse(cleanString, out value))
                    {
                        logger.LogWarning("Could not parse {FieldName} string value '{OriginalValue}' (cleaned: '{CleanedValue}') for '{LocationName}' at row {Row} - setting to null", 
                            fieldName, stringValue, cleanString, locationName, row);
                        return null;
                    }
                    break;
                default:
                    // Try generic conversion as fallback
                    var fallbackString = cellString.Replace(",", "").Replace(" ", "").Trim();
                    if (!int.TryParse(fallbackString, out value))
                    {
                        logger.LogWarning("Could not parse {FieldName} value '{Value}' (type: {Type}) for '{LocationName}' at row {Row} - setting to null", 
                            fieldName, cellValue, cellValue.GetType().Name, locationName, row);
                        return null;
                    }
                    break;
            }

            // Validate the parsed value
            if (value < 0)
            {
                logger.LogWarning("Negative {FieldName} value ({Value}) for '{LocationName}' at row {Row} - setting to null", 
                    fieldName, value, locationName, row);
                return null;
            }

            // Log successful parsing for debugging
            logger.LogTrace("Successfully parsed {FieldName} value '{OriginalValue}' -> {ParsedValue} for '{LocationName}' at row {Row}", 
                fieldName, cellString, value, locationName, row);

            return value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception parsing {FieldName} value '{Value}' (type: {Type}) for '{LocationName}' at row {Row} - setting to null", 
                fieldName, cellValue, cellValue?.GetType().Name ?? "null", locationName, row);
            return null;
        }
    }

    private static int? ParseHouseholdValue(object? cellValue, string fieldName, string locationName, int row, ILogger logger)
    {
        // Use the same improved logic as ParsePopulationValue
        return ParsePopulationValue(cellValue, fieldName, locationName, row, logger);
    }

    private static void LogSeedingResults(PopulationSeedingStats stats, ILogger logger)
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

public class PopulationSeedingStats
{
    // Regions
    public int RegionsMatched { get; set; } = 0;
    public int RegionsNotFound { get; set; } = 0;
    public int RegionsErrors { get; set; } = 0;

    // Provinces
    public int ProvincesMatched { get; set; } = 0;
    public int ProvincesNotFound { get; set; } = 0;
    public int ProvincesRegionNotFound { get; set; } = 0;
    public int ProvincesErrors { get; set; } = 0;

    // Municipalities  
    public int MunicipalitiesMatched { get; set; } = 0;
    public int MunicipalitiesNotFound { get; set; } = 0;
    public int MunicipalitiesRegionNotFound { get; set; } = 0;
    public int MunicipalitiesErrors { get; set; } = 0;

    // Helper properties
    public bool HasRegionData => RegionsMatched > 0 || RegionsNotFound > 0 || RegionsErrors > 0;
    public bool HasProvinceData => ProvincesMatched > 0 || ProvincesNotFound > 0 || ProvincesRegionNotFound > 0 || ProvincesErrors > 0;
    public bool HasMunicipalityData => MunicipalitiesMatched > 0 || MunicipalitiesNotFound > 0 || 
                                       MunicipalitiesRegionNotFound > 0 || MunicipalitiesErrors > 0;
}