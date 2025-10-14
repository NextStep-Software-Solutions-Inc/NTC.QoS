using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
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
    public static async Task SeedAllAsync(ApplicationDbContext context, IBlobStorageService blobStorageService, ILogger logger)
    {
        logger.LogInformation("Starting full database seeding process...");

        await SeedRegionsAsync(context, blobStorageService, logger);
        await SeedProvincesAsync(context, blobStorageService, logger);
        await SeedMunicipalitiesAsync(context, blobStorageService, logger);
        await SeedBarangaysAsync(context, blobStorageService, logger);

        logger.LogInformation("Full database seeding process completed.");
    }


    /// <summary>
    /// Seeds Region entities from the GeoJSON file in blob storage.
    /// This logic is adapted from the previous RegionSeeder.
    /// </summary>
    public static async Task SeedRegionsAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger)
    {
        // if (await dbContext.Regions.AnyAsync())
        // {
        //     logger.LogInformation("Database already contains regions. Skipping region seeding.");
        //     return;
        // }

        logger.LogInformation("Starting region seeding from blob storage: {ContainerName}/{FilePath}...", ContainerName, RegionGeoJsonFilePath);

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            // Check if the file exists in blob storage
            if (!await blobStorageService.ExistsAsync(ContainerName, RegionGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, RegionGeoJsonFilePath);
                return;
            }

            // Download the file from blob storage
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
            .Where(region => !string.IsNullOrWhiteSpace(region.code) && !existingRegionCodes.Contains(region.code))
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
    private static async Task SeedProvincesAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger)
    {
        if (await dbContext.Provinces.AnyAsync())
        {
            logger.LogInformation("Database already contains provinces. Skipping province seeding.");
            return;
        }
        logger.LogInformation("Starting province seeding from blob storage: {ContainerName}/{FilePath}...", ContainerName, ProvinceGeoJsonFilePath);

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            // Check if the file exists in blob storage
            if (!await blobStorageService.ExistsAsync(ContainerName, ProvinceGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, ProvinceGeoJsonFilePath);
                return;
            }

            // Download the file from blob storage
            using var geoJsonStream = await blobStorageService.DownloadAsync(ContainerName, ProvinceGeoJsonFilePath);
            using var streamReader = new StreamReader(geoJsonStream);
            string geoJsonText = await streamReader.ReadToEndAsync();

            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(geoJsonText)))
            {
                featureCollection = serializer.Deserialize<FeatureCollection>(stringReader) ?? new FeatureCollection();
            }

            if (featureCollection == null || !featureCollection.Any())
            {
                logger.LogWarning("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error reading or parsing GeoJSON file for provinces from blob storage: {ErrorMessage}", ex.Message);
            logger.LogTrace(ex.StackTrace);
            return;
        }

        var regionMap = await dbContext.Regions.Select(r => new { r.Code, r.Id }).ToDictionaryAsync(r => r.Code, r => r.Id);

        var provincesToAdd = new List<Province>();

        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogInformation("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var provinceName = feature.Attributes["ADM2_EN"]?.ToString() ?? string.Empty;

            var province = new Province
            {
                Name = provinceName,
                Description = null,
                Code = feature.Attributes["ADM2_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f,
                TotalPopulation = ParsePopulationValue(feature.Attributes["TOTAL_POP"], logger, provinceName),
                HouseholdPopulation = ParsePopulationValue(feature.Attributes["HHOLD_POPN"], logger, provinceName),
                HouseholdCount = ParsePopulationValue(feature.Attributes["HHOLD_CNT"], logger, provinceName)
            };

            switch (feature.Geometry)
            {
                case MultiPolygon geoJsonMultiPolygon:
                    province.Boundaries = geoJsonMultiPolygon;
                    province.Center = geoJsonMultiPolygon.Centroid;
                    break;
                case Polygon geoJsonPolygon:
                    province.Boundaries = GeometryHelper.CreateMultiPolygon(null, geoJsonPolygon);
                    province.Center = geoJsonPolygon.Centroid;
                    break;
                default:
                    logger.LogInformation(
                        "Terminated '{Name}' due to unsupported geometry type: {GeometryType}",
                        province.Name,
                        feature.Geometry.GeometryType
                    );
                    return;
            }

            var regionCode = feature.Attributes["ADM1_PCODE"]?.ToString();
            if (!string.IsNullOrWhiteSpace(regionCode) && regionMap.TryGetValue(regionCode, out var regionId))
            {
                province.RegionId = regionId;
            }
            else
            {
                logger.LogWarning(
                    "Warning: Could not find Region for province '{Name}' with ADM1_PCODE '{Code}'",
                    province.Name,
                    regionCode
                );
            }
            provincesToAdd.Add(province);
        }

        try
        {
            await dbContext.BulkInsertAsync(provincesToAdd);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {Count} provinces into the database.", provincesToAdd.Count);
        }
        catch (Exception ex)
        {
            logger.LogError("Error saving provinces to database: {Message}", ex.Message);
            logger.LogInformation(ex.InnerException?.Message);
            logger.LogTrace(ex.StackTrace);
        }
    }

    /// <summary>
    /// Seeds Municipality entities from the GeoJSON file in blob storage.
    /// </summary>
    private static async Task SeedMunicipalitiesAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger)
    {
        if (await dbContext.Municipalities.AnyAsync())
        {
            logger.LogInformation("Database already contains municipalities. Skipping municipality seeding.");
            return;
        }
        logger.LogInformation("Starting municipalities seeding from blob storage: {ContainerName}/{FilePath}...", ContainerName, MunicipalityGeoJsonFilePath);

        FeatureCollection featureCollection = [];

        try
        {
            // Check if the file exists in blob storage
            if (!await blobStorageService.ExistsAsync(ContainerName, MunicipalityGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, MunicipalityGeoJsonFilePath);
                return;
            }

            // Download the file from blob storage
            using var geoJsonStream = await blobStorageService.DownloadAsync(ContainerName, MunicipalityGeoJsonFilePath);
            using var streamReader = new StreamReader(geoJsonStream);
            string geoJsonText = await streamReader.ReadToEndAsync();

            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(geoJsonText)))
            {
                featureCollection = serializer.Deserialize<FeatureCollection>(stringReader) ?? new FeatureCollection();
            }

            if (featureCollection == null || !featureCollection.Any())
            {
                logger.LogInformation("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error reading or parsing GeoJSON file for municipalities from blob storage: {Message}", ex.Message);
            logger.LogTrace(ex.StackTrace);
            return;
        }

        var provinceMap = await dbContext.Provinces.Select(p => new { p.Code, p.Id }).ToDictionaryAsync(p => p.Code, p => p.Id);

        var municipalitiesToAdd = new List<Municipality>();

       foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogInformation("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var municipalityName = feature.Attributes["ADM3_EN"]?.ToString() ?? string.Empty;

            var municipality = new Municipality
            {
                Name = municipalityName,
                Description = null,
                Code = feature.Attributes["ADM3_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f,
                TotalPopulation = ParsePopulationValue(feature.Attributes["TOTAL_POP"], logger, municipalityName),
                HouseholdPopulation = ParsePopulationValue(feature.Attributes["HHOLD_POPN"], logger, municipalityName),
                HouseholdCount = ParsePopulationValue(feature.Attributes["HHOLD_CNT"], logger, municipalityName)
            };

            switch (feature.Geometry)
            {
                case MultiPolygon geoJsonMultiPolygon:
                    municipality.Boundaries = geoJsonMultiPolygon;
                    municipality.Center = geoJsonMultiPolygon.Centroid;
                    break;
                case Polygon geoJsonPolygon:
                    municipality.Boundaries = GeometryHelper.CreateMultiPolygon(null, geoJsonPolygon);
                    municipality.Center = geoJsonPolygon.Centroid;
                    break;
                default:
                    logger.LogInformation(
                        "Terminated '{Name}' due to unsupported geometry type: {GeometryType}",
                        municipality.Name,
                        feature.Geometry.GeometryType
                    );
                    continue;
            }

            var provinceCode = feature.Attributes["ADM2_PCODE"]?.ToString();
            if (!string.IsNullOrWhiteSpace(provinceCode) && provinceMap.TryGetValue(provinceCode, out var provinceId))
            {
                municipality.ProvinceId = provinceId;
            }
            else
            {
                logger.LogWarning(
                    "Warning: Could not find Province for municipality '{Name}' with ADM2_PCODE '{Code}'",
                    municipality.Name,
                    provinceCode
                );
                continue;
            }

            municipalitiesToAdd.Add(municipality);
        }

        try
        {
            await dbContext.BulkInsertAsync(municipalitiesToAdd);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {Count} municipalities into the database.", municipalitiesToAdd.Count);
        }
        catch (Exception ex)
        {
            logger.LogError("Error saving municipalities to database: {Message}", ex.Message);
            logger.LogInformation(ex.InnerException?.Message);
            logger.LogTrace(ex.StackTrace);
        }
    }

    /// <summary>
    /// Seeds Barangay entities from the GeoJSON file in blob storage.
    /// </summary>
    private static async Task SeedBarangaysAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger)
    {
        if (await dbContext.Barangays.AnyAsync())
        {
            logger.LogInformation("Database already contains barangays. Skipping barangay seeding.");
            return;
        }
        logger.LogInformation("Starting barangays seeding from blob storage: {ContainerName}/{FilePath}...", ContainerName, BarangayGeoJsonFilePath);

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            // Check if the file exists in blob storage
            if (!await blobStorageService.ExistsAsync(ContainerName, BarangayGeoJsonFilePath))
            {
                logger.LogError("GeoJSON file not found in blob storage: {ContainerName}/{FilePath}", ContainerName, BarangayGeoJsonFilePath);
                return;
            }

            // Download the file from blob storage
            using var geoJsonStream = await blobStorageService.DownloadAsync(ContainerName, BarangayGeoJsonFilePath);
            using var streamReader = new StreamReader(geoJsonStream);
            string geoJsonText = await streamReader.ReadToEndAsync();

            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(geoJsonText)))
            {
                featureCollection = serializer.Deserialize<FeatureCollection>(stringReader) ?? new FeatureCollection();
            }

            if (featureCollection == null || !featureCollection.Any())
            {
                logger.LogInformation("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading or parsing GeoJSON file for barangays from blob storage: {Message}", ex.Message);
            logger.LogTrace(ex.StackTrace);
            return;
        }

        var municipalityMap = await dbContext.Municipalities.Select(m => new { m.Code, m.Id }).ToDictionaryAsync(m => m.Code, m => m.Id);

        var barangaysToAdd = new List<Barangay>();

        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogInformation("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var barangay = new Barangay
            {
                Name = feature.Attributes["ADM4_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM4_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f,
                TotalPopulation = null,
                HouseholdPopulation = null,
                HouseholdCount = null
            };

            switch (feature.Geometry)
            {
                case MultiPolygon geoJsonMultiPolygon:
                    barangay.Boundaries = geoJsonMultiPolygon;
                    barangay.Center = geoJsonMultiPolygon.Centroid;
                    break;
                case Polygon geoJsonPolygon:
                    barangay.Boundaries = GeometryHelper.CreateMultiPolygon(null, geoJsonPolygon);
                    barangay.Center = geoJsonPolygon.Centroid;
                    break;
                default:
                    logger.LogInformation(
                        "Terminated '{Name}' due to unsupported geometry type: {GeometryType}",
                        barangay.Name,
                        feature.Geometry.GeometryType
                    );
                    continue;
            }

            var municipalityCode = feature.Attributes["ADM3_PCODE"]?.ToString();
            if (!string.IsNullOrWhiteSpace(municipalityCode) && municipalityMap.TryGetValue(municipalityCode, out var municipalityId))
            {
                barangay.MunicipalityId = municipalityId;
            }
            else
            {
                logger.LogWarning(
                    "Warning: Could not find Municipality for barangay '{Name}' with ADM3_PCODE '{Code}'",
                    barangay.Name,
                    municipalityCode
                );
                continue;
            }

            barangaysToAdd.Add(barangay);
        }

        try
        {
            await dbContext.BulkInsertAsync(barangaysToAdd);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {Count} barangays into the database.", barangaysToAdd.Count);
        }
        catch (Exception ex)
        {
            logger.LogError("Error saving barangays to database: {Message}", ex.Message);
            logger.LogInformation(ex.InnerException?.Message);
            logger.LogTrace(ex.StackTrace);
        }
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
