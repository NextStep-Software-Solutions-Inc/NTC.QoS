using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class LocationSeeder
{
    private const string ContainerName = "seeds";
    private const string RegionGeoJsonFilePath = "boundaries/Regionbdry.geojson";
    private const string ProvinceGeoJsonFilePath = "boundaries/Provincebdry.geojson";
    private const string MunicipalityGeoJsonFilePath = "boundaries/Municipalitybdry.geojson";
    private const string BarangayGeoJsonFilePath = "boundaries/Barangaybdry.geojson";

    /// <summary>
    /// Seeds all configured entities into the database.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="blobStorageService">The blob storage service instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="forceReseed">If true, forces reseeding even if data exists. If false, skips seeding when data is present.</param>
    public static async Task SeedAllAsync(ApplicationDbContext context, IBlobStorageService blobStorageService, ILogger logger, bool forceReseed = false)
    {
        logger.LogInformation("Starting database seeding process... ForceReseed: {ForceReseed}", forceReseed);

        // ALWAYS check if data already exists as a safety measure
        // Even with forceReseed=true, we should not duplicate data
        var hasExistingData = await HasExistingLocationDataAsync(context, logger);
        
        if (hasExistingData && !forceReseed)
        {
            logger.LogInformation("Location data already exists in database. Skipping seeding process. Use forceReseed=true to override.");
            return;
        }
        
        if (hasExistingData && forceReseed)
        {
            logger.LogWarning("⚠️ ForceReseed is enabled but data already exists. This should only be used for data corrections, not regular seeding.");
            logger.LogWarning("⚠️ Existing data counts - Regions: will be checked individually to avoid duplicates");
        }

        await SeedRegionsAsync(context, blobStorageService, logger, forceReseed);
        await SeedProvincesAsync(context, blobStorageService, logger, forceReseed);
        await SeedMunicipalitiesAsync(context, blobStorageService, logger, forceReseed);
        await SeedBarangaysAsync(context, blobStorageService, logger, forceReseed);

        logger.LogInformation("Database seeding process completed.");
    }

    /// <summary>
    /// Checks if location data already exists in the database.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>True if location data exists, false otherwise.</returns>
    private static async Task<bool> HasExistingLocationDataAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            var regionCount = await context.Regions.CountAsync();
            var provinceCount = await context.Provinces.CountAsync();
            var municipalityCount = await context.Municipalities.CountAsync();
            var barangayCount = await context.Barangays.CountAsync();

            var hasData = regionCount > 0 || provinceCount > 0 || municipalityCount > 0 || barangayCount > 0;

            logger.LogInformation("Existing location data check - Regions: {RegionCount}, Provinces: {ProvinceCount}, Municipalities: {MunicipalityCount}, Barangays: {BarangayCount}", 
                regionCount, provinceCount, municipalityCount, barangayCount);

            return hasData;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking existing location data. Proceeding with seeding.");
            return false;
        }
    }

    /// <summary>
    /// Seeds Region entities from the GeoJSON file in blob storage.
    /// This logic is adapted from the previous RegionSeeder.
    /// </summary>
    /// <param name="forceReseed">If true, forces reseeding even if data exists.</param>
    public static async Task SeedRegionsAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger, bool forceReseed = false)
    {
        logger.LogInformation("Starting region seeding from blob storage: {ContainerName}/{FilePath}... ForceReseed: {ForceReseed}", ContainerName, RegionGeoJsonFilePath, forceReseed);

        // Check if regions already exist when forceReseed is false
        if (!forceReseed)
        {
            var existingRegionCount = await dbContext.Regions.CountAsync();
            if (existingRegionCount > 0)
            {
                logger.LogInformation("Regions already exist in database ({Count} found). Skipping region seeding.", existingRegionCount);
                return;
            }
        }

        FeatureCollection featureCollection = [];

        try
        {
            if (!await blobStorageService.ExistsAsync(ContainerName, RegionGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, RegionGeoJsonFilePath);
                return;
            }

            using var geoJsonStream = await blobStorageService.DownloadAsync(ContainerName, RegionGeoJsonFilePath);
            using var streamReader = new StreamReader(geoJsonStream);
            var serializer = GeoJsonSerializer.Create();
            using var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
            featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader) ?? [];

            if (featureCollection == null || !featureCollection.Any())
            {
                logger.LogInformation("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading or parsing GeoJSON file for regions from blob storage: {ErrorMessage}", ex.Message);
            return;
        }

        var existingRegionCodes = await dbContext.Regions.AsNoTracking().Select(r => r.Code).ToHashSetAsync();
        var regionsToAdd = featureCollection
            .Where(f => f?.Attributes != null &&
                f.Attributes["ADM1_PCODE"] != null &&
                f.Attributes["ADM1_EN"] != null &&
                f.Attributes["AREA_SQKM"] != null &&
                f.Attributes["TOTAL_POP"] != null &&
                f.Attributes["HHOLD_POPN"] != null &&
                f.Attributes["HHOLD_CNT"] != null)
            .Select(f => new
            {
                code = f.Attributes["ADM1_PCODE"]?.ToString(),
                name = f.Attributes["ADM1_EN"]?.ToString(),
                area = float.TryParse(f.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f,
                totalPopulation = ParsePopulationValue(f.Attributes["TOTAL_POP"], logger, f.Attributes["ADM1_EN"]?.ToString() ?? string.Empty),
                householdPopulation = ParsePopulationValue(f.Attributes["HHOLD_POPN"], logger, f.Attributes["ADM1_EN"]?.ToString() ?? string.Empty),
                householdCount = ParsePopulationValue(f.Attributes["HHOLD_CNT"], logger, f.Attributes["ADM1_EN"]?.ToString() ?? string.Empty),
                boundaries = GetBoundaries(f)
                
            })
            .Where(region => !string.IsNullOrWhiteSpace(region.code) && (forceReseed || !existingRegionCodes.Contains(region.code)))
            .Select(region => new Region
            {
                Name = region.name ?? string.Empty,
                Description = null,
                Code = region.code ?? string.Empty,
                Area = region.area,
                TotalPopulation = region.totalPopulation,
                HouseholdPopulation = region.householdPopulation,
                HouseholdCount = region.householdCount,
                Boundaries = region.boundaries,
                Center = region.boundaries.Centroid
            })
            .ToList();

        if (regionsToAdd.Count == 0)
        {
            logger.LogInformation("No new regions to seed. All regions from the GeoJSON already exist in the database.");
            return;
        }

        await dbContext.BulkInsertAsync(regionsToAdd);
        
        logger.LogInformation("Region seeding completed with {Count} regions added.", regionsToAdd.Count);
    }

    internal static MultiPolygon GetBoundaries(IFeature feature)
    {
        return feature.Geometry switch
        {
            MultiPolygon geoJsonMultiPolygon => geoJsonMultiPolygon,
            Polygon geoJsonPolygon => GeometryHelper.CreateMultiPolygon(null, geoJsonPolygon),
            _ => throw new InvalidOperationException("Unsupported geometry type for boundaries. Expected MultiPolygon or Polygon."),
        };
    }
  
    /// <summary>
    /// Seeds Province entities from the GeoJSON file in blob storage.
    /// </summary>
    /// <param name="forceReseed">If true, forces reseeding even if data exists.</param>
    private static async Task SeedProvincesAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger, bool forceReseed = false)
    {
        logger.LogInformation("Starting province seeding from blob storage: {ContainerName}/{FilePath}... ForceReseed: {ForceReseed}", ContainerName, ProvinceGeoJsonFilePath, forceReseed);

        // Check if provinces already exist when forceReseed is false
        if (!forceReseed)
        {
            var existingProvinceCount = await dbContext.Provinces.CountAsync();
            if (existingProvinceCount > 0)
            {
                logger.LogInformation("Provinces already exist in database ({Count} found). Skipping province seeding.", existingProvinceCount);
                return;
            }
        }

        FeatureCollection featureCollection = [];

        try
        {
            if (!await blobStorageService.ExistsAsync(ContainerName, ProvinceGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, ProvinceGeoJsonFilePath);
                return;
            }

            using var geoJsonStream = await blobStorageService.DownloadAsync(ContainerName, ProvinceGeoJsonFilePath);
            using var streamReader = new StreamReader(geoJsonStream);
            var serializer = GeoJsonSerializer.Create();
            using var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
            featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader) ?? [];

            if (featureCollection == null || !featureCollection.Any())
            {
                logger.LogInformation("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading or parsing GeoJSON file for provinces from blob storage: {ErrorMessage}", ex.Message);
            return;
        }

        var regionMap = await dbContext.Regions.AsNoTracking().Select(r => new { r.Code, r.Id }).ToDictionaryAsync(r => r.Code, r => r.Id);
        var existingProvinceCodes = await dbContext.Provinces.AsNoTracking().Select(p => p.Code).ToHashSetAsync();
        
        var provincesToAdd = featureCollection
            .Where(f => f?.Attributes != null &&
                f.Attributes["ADM2_PCODE"] != null &&
                f.Attributes["ADM2_EN"] != null &&
                f.Attributes["AREA_SQKM"] != null &&
                f.Attributes["ADM1_PCODE"] != null)
            .Select(f => new
            {
                code = f.Attributes["ADM2_PCODE"]?.ToString(),
                name = f.Attributes["ADM2_EN"]?.ToString(),
                area = float.TryParse(f.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f,
                totalPopulation = ParsePopulationValue(f.Attributes["TOTAL_POP"], logger, f.Attributes["ADM2_EN"]?.ToString() ?? string.Empty),
                householdPopulation = ParsePopulationValue(f.Attributes["HHOLD_POPN"], logger, f.Attributes["ADM2_EN"]?.ToString() ?? string.Empty),
                householdCount = ParsePopulationValue(f.Attributes["HHOLD_CNT"], logger, f.Attributes["ADM2_EN"]?.ToString() ?? string.Empty),
                boundaries = GetBoundaries(f),
                regionCode = f.Attributes["ADM1_PCODE"]?.ToString()
            })
            .Where(province => !string.IsNullOrWhiteSpace(province.code) && 
                (forceReseed || !existingProvinceCodes.Contains(province.code)) &&
                !string.IsNullOrWhiteSpace(province.regionCode) && 
                regionMap.ContainsKey(province.regionCode))
            .Select(province => new Province
            {
                Name = province.name ?? string.Empty,
                Description = null,
                Code = province.code ?? string.Empty,
                Area = province.area,
                TotalPopulation = province.totalPopulation,
                HouseholdPopulation = province.householdPopulation,
                HouseholdCount = province.householdCount,
                Boundaries = province.boundaries,
                Center = province.boundaries.Centroid,
                RegionId = regionMap[province.regionCode!]
            })
            .ToList();

        if (provincesToAdd.Count == 0)
        {
            logger.LogInformation("No new provinces to seed. All provinces from the GeoJSON already exist in the database or have missing parent regions.");
            return;
        }

        await dbContext.BulkInsertAsync(provincesToAdd);
        
        logger.LogInformation("Province seeding completed with {Count} provinces added.", provincesToAdd.Count);
    }

    /// <summary>
    /// Seeds Municipality entities from the GeoJSON file in blob storage.
    /// </summary>
    /// <param name="forceReseed">If true, forces reseeding even if data exists.</param>
    private static async Task SeedMunicipalitiesAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger, bool forceReseed = false)
    {
        logger.LogInformation("Starting municipality seeding from blob storage: {ContainerName}/{FilePath}... ForceReseed: {ForceReseed}", ContainerName, MunicipalityGeoJsonFilePath, forceReseed);

        // Check if municipalities already exist when forceReseed is false
        if (!forceReseed)
        {
            var existingMunicipalityCount = await dbContext.Municipalities.CountAsync();
            if (existingMunicipalityCount > 0)
            {
                logger.LogInformation("Municipalities already exist in database ({Count} found). Skipping municipality seeding.", existingMunicipalityCount);
                return;
            }
        }

        FeatureCollection featureCollection = [];

        try
        {
            if (!await blobStorageService.ExistsAsync(ContainerName, MunicipalityGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, MunicipalityGeoJsonFilePath);
                return;
            }

            using var geoJsonStream = await blobStorageService.DownloadAsync(ContainerName, MunicipalityGeoJsonFilePath);
            using var streamReader = new StreamReader(geoJsonStream);
            var serializer = GeoJsonSerializer.Create();
            using var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
            featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader) ?? [];

            if (featureCollection == null || !featureCollection.Any())
            {
                logger.LogInformation("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading or parsing GeoJSON file for municipalities from blob storage: {ErrorMessage}", ex.Message);
            return;
        }

        var provinceMap = await dbContext.Provinces.AsNoTracking().Select(p => new { p.Code, p.Id }).ToDictionaryAsync(p => p.Code, p => p.Id);
        var existingMunicipalityCodes = await dbContext.Municipalities.AsNoTracking().Select(m => m.Code).ToHashSetAsync();

        var municipalitiesToAdd = featureCollection
            .Where(f => f?.Attributes != null &&
                f.Attributes["ADM3_PCODE"] != null &&
                f.Attributes["ADM3_EN"] != null &&
                f.Attributes["AREA_SQKM"] != null &&
                f.Attributes["ADM2_PCODE"] != null)
            .Select(f => new
            {
                code = f.Attributes["ADM3_PCODE"]?.ToString(),
                name = f.Attributes["ADM3_EN"]?.ToString(),
                area = float.TryParse(f.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f,
                totalPopulation = ParsePopulationValue(f.Attributes["TOTAL_POP"], logger, f.Attributes["ADM3_EN"]?.ToString() ?? string.Empty),
                householdPopulation = ParsePopulationValue(f.Attributes["HHOLD_POPN"], logger, f.Attributes["ADM3_EN"]?.ToString() ?? string.Empty),
                householdCount = ParsePopulationValue(f.Attributes["HHOLD_CNT"], logger, f.Attributes["ADM3_EN"]?.ToString() ?? string.Empty),
                boundaries = GetBoundaries(f),
                provinceCode = f.Attributes["ADM2_PCODE"]?.ToString()
            })
            .Where(municipality => !string.IsNullOrWhiteSpace(municipality.code) && 
                (forceReseed || !existingMunicipalityCodes.Contains(municipality.code)) &&
                !string.IsNullOrWhiteSpace(municipality.provinceCode) && 
                provinceMap.ContainsKey(municipality.provinceCode))
            .Select(municipality => new Municipality
            {
                Name = municipality.name ?? string.Empty,
                Description = null,
                Code = municipality.code ?? string.Empty,
                Area = municipality.area,
                TotalPopulation = municipality.totalPopulation,
                HouseholdPopulation = municipality.householdPopulation,
                HouseholdCount = municipality.householdCount,
                Boundaries = municipality.boundaries,
                Center = municipality.boundaries.Centroid,
                ProvinceId = provinceMap[municipality.provinceCode!]
            })
            .ToList();

        if (municipalitiesToAdd.Count == 0)
        {
            logger.LogInformation("No new municipalities to seed. All municipalities from the GeoJSON already exist in the database or have missing parent provinces.");
            return;
        }

        await dbContext.BulkInsertAsync(municipalitiesToAdd);
        
        logger.LogInformation("Municipality seeding completed with {Count} municipalities added.", municipalitiesToAdd.Count);
    }

    /// <summary>
    /// Seeds Barangay entities from the GeoJSON file in blob storage.
    /// </summary>
    /// <param name="forceReseed">If true, forces reseeding even if data exists.</param>
    private static async Task SeedBarangaysAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger, bool forceReseed = false)
    {
        logger.LogInformation("Starting barangay seeding from blob storage: {ContainerName}/{FilePath}... ForceReseed: {ForceReseed}", ContainerName, BarangayGeoJsonFilePath, forceReseed);

        // Check if barangays already exist when forceReseed is false
        if (!forceReseed)
        {
            var existingBarangayCount = await dbContext.Barangays.CountAsync();
            if (existingBarangayCount > 0)
            {
                logger.LogInformation("Barangays already exist in database ({Count} found). Skipping barangay seeding.", existingBarangayCount);
                return;
            }
        }

        FeatureCollection featureCollection = [];

        try
        {
            if (!await blobStorageService.ExistsAsync(ContainerName, BarangayGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, BarangayGeoJsonFilePath);
                return;
            }

            using var geoJsonStream = await blobStorageService.DownloadAsync(ContainerName, BarangayGeoJsonFilePath);
            using var streamReader = new StreamReader(geoJsonStream);
            var serializer = GeoJsonSerializer.Create();
            using var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
            featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader) ?? [];

            if (featureCollection == null || !featureCollection.Any())
            {
                logger.LogInformation("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading or parsing GeoJSON file for barangays from blob storage: {ErrorMessage}", ex.Message);
            return;
        }

        var municipalityMap = await dbContext.Municipalities.AsNoTracking().Select(m => new { m.Code, m.Id }).ToDictionaryAsync(m => m.Code, m => m.Id);
        var existingBarangayCodes = await dbContext.Barangays.AsNoTracking().Select(b => b.Code).ToHashSetAsync();

        var barangaysToAdd = featureCollection
            .Where(f => f?.Attributes != null &&
                f.Attributes["ADM4_PCODE"] != null &&
                f.Attributes["ADM4_EN"] != null &&
                f.Attributes["AREA_SQKM"] != null &&
                f.Attributes["ADM3_PCODE"] != null)
            .Select(f => new
            {
                code = f.Attributes["ADM4_PCODE"]?.ToString(),
                name = f.Attributes["ADM4_EN"]?.ToString(),
                area = float.TryParse(f.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f,
                boundaries = GetBoundaries(f),
                municipalityCode = f.Attributes["ADM3_PCODE"]?.ToString()
            })
            .Where(barangay => !string.IsNullOrWhiteSpace(barangay.code) && 
                (forceReseed || !existingBarangayCodes.Contains(barangay.code)) &&
                !string.IsNullOrWhiteSpace(barangay.municipalityCode) && 
                municipalityMap.ContainsKey(barangay.municipalityCode))
            .Select(barangay => new Barangay
            {
                Name = barangay.name ?? string.Empty,
                Description = null,
                Code = barangay.code ?? string.Empty,
                Area = barangay.area,
                TotalPopulation = null,
                HouseholdPopulation = null,
                HouseholdCount = null,
                Boundaries = barangay.boundaries,
                Center = barangay.boundaries.Centroid,
                MunicipalityId = municipalityMap[barangay.municipalityCode!]
            })
            .ToList();

        if (barangaysToAdd.Count == 0)
        {
            logger.LogInformation("No new barangays to seed. All barangays from the GeoJSON already exist in the database or have missing parent municipalities.");
            return;
        }

        await dbContext.BulkInsertAsync(barangaysToAdd);
        
        logger.LogInformation("Barangay seeding completed with {Count} barangays added.", barangaysToAdd.Count);
    }

    /// <summary>
    /// Safely parses a population value from a GeoJSON attribute object.
    /// </summary>
    /// <param name="attributeValue">The attribute value object.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="locationName">The name of the location for logging context.</param>
    /// <returns>A nullable long with the parsed value, or null if parsing fails.</returns>
    private static int? ParsePopulationValue(object? attributeValue, ILogger logger, string locationName)
    {
        if (attributeValue == null)
        {
            return null;
        }

        var valueStr = attributeValue.ToString();
        if (string.IsNullOrWhiteSpace(valueStr))
        {
            return null;
        }

        var cleanedValueStr = valueStr.Replace(",", "");

        if (int.TryParse(cleanedValueStr, out int result))
        {
            return result;
        }
        else
        {
            logger.LogWarning("Could not parse population value '{Value}' for '{Location}'. Setting to null.", valueStr, locationName);
            return null;
        }
    }
}
