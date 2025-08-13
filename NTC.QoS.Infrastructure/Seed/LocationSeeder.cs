using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class LocationSeeder
{
    private const string RegionGeoJsonFilePath = "admin1.geojson";
    private const string ProvinceGeoJsonFilePath = "admin2.geojson";
    private const string MunicipalityGeoJsonFilePath = "admin3.geojson";
    private const string BarangayGeoJsonFilePath = "admin4.geojson";

    /// <summary>
    /// Seeds all configured entities into the database.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    public static async Task SeedAllAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting full database seeding process...");

        await SeedRegionsAsync(context, logger);
        await SeedProvincesAsync(context, logger);
        await SeedMunicipalitiesAsync(context, logger);
        await SeedBarangaysAsync(context, logger);

        logger.LogInformation("Full database seeding process completed.");
    }

    /// <summary>
    /// Seeds a specific entity type into the database based on the provided enum.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="entityType">The type of entity to seed.</param>
    public static async Task SeedEntityAsync(ApplicationDbContext context, LocationTypeToSeedEnum entityType, ILogger logger)
    {
        logger.LogInformation("Starting seeding for entity type: {EntityType}...", entityType);

        switch (entityType)
        {
            case LocationTypeToSeedEnum.All:
                await SeedAllAsync(context, logger);
                break;
            case LocationTypeToSeedEnum.Region:
                await SeedRegionsAsync(context, logger);
                break;
            case LocationTypeToSeedEnum.Province:
                await SeedProvincesAsync(context, logger);
                break;
            case LocationTypeToSeedEnum.Municipality:
                await SeedMunicipalitiesAsync(context, logger);
                break;
            case LocationTypeToSeedEnum.Barangay:
                await SeedBarangaysAsync(context, logger);
                break;
            default:
                logger.LogInformation("Unknown entity type '{EntityType}'. No seeding performed.", entityType);
                break;
        }

        logger.LogInformation("Seeding for entity type '{EntityType}' completed.", entityType);
    }

    /// <summary>
    /// Seeds Region entities from the GeoJSON file.
    /// This logic is adapted from the previous RegionSeeder.
    /// </summary>
    public static async Task SeedRegionsAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Regions.AnyAsync())
        {
            logger.LogInformation("Database already contains regions. Skipping region seeding.");
            return;
        }

        logger.LogInformation("Starting region seeding from {FilePath}...", RegionGeoJsonFilePath);

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, RegionGeoJsonFilePath);

            if (!System.IO.File.Exists(fullGeoJsonPath))
            {
                logger.LogInformation("Error: GeoJSON file not found at {FilePath}. Make sure it's copied to output directory.", fullGeoJsonPath);
                return;
            }

            string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
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
            logger.LogInformation("Error reading or parsing GeoJSON file for regions: {ErrorMessage}", ex.Message);
            return;
        }

        // save the regions 
        var regionsToAdd = new List<Region>();
        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogInformation("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var region = new Region
            {
                Name = feature.Attributes["ADM1_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM1_PCODE"]?.ToString() ?? string.Empty
            };

            switch (feature.Geometry)
            {
                case MultiPolygon geoJsonMultiPolygon:
                    region.Boundaries = geoJsonMultiPolygon;
                    region.Center = geoJsonMultiPolygon.Centroid;
                    break;
                case Polygon geoJsonPolygon:
                    region.Boundaries = GeometryHelper.CreateMultiPolygon(null, geoJsonPolygon);
                    region.Center = geoJsonPolygon.Centroid;
                    break;
                default:
                    logger.LogInformation(
                        "Terminated '{Name}' due to unsupported geometry type: {GeometryType}",
                        region.Name,
                        feature.Geometry.GeometryType
                    );
                    return;
            }

            regionsToAdd.Add(region);
        }

        await dbContext.Regions.AddRangeAsync(regionsToAdd);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Region seeding completed with {Count} regions added.", regionsToAdd.Count);
    }

    /// <summary>
    /// Placeholder for seeding Province entities.
    /// This method will be expanded once GeoJSON format for Provinces is provided.
    /// </summary>
    private static async Task SeedProvincesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Provinces.AnyAsync())
        {
            logger.LogInformation("Database already contains provinces. Skipping region seeding.");
            return;
        }
        logger.LogInformation("Starting province seeding from {FilePath}...", ProvinceGeoJsonFilePath);

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, ProvinceGeoJsonFilePath);

            if (!System.IO.File.Exists(fullGeoJsonPath))
            {
                logger.LogError("Error: GeoJSON file not found at {fullGeoJsonPath}. Make sure it's copied to output directory.", fullGeoJsonPath);
                return;
            }

            string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
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
            logger.LogError("Error reading or parsing GeoJSON file for provinces: {ErrorMessage}", ProvinceGeoJsonFilePath);
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

            var province = new Province
            {
                Name = feature.Attributes["ADM2_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM2_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
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
            await dbContext.Provinces.AddRangeAsync(provincesToAdd);
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
    /// Placeholder for seeding Municipality entities.
    /// This method will be expanded once GeoJSON format for Municipalities is provided.
    /// </summary>
    private static async Task SeedMunicipalitiesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Municipalities.AnyAsync())
        {
            logger.LogInformation("Database already contains municipalities. Skipping municipality seeding.");
            return;
        }
        logger.LogInformation("Starting municipalities seeding from {FilePath}...", MunicipalityGeoJsonFilePath);

        FeatureCollection featureCollection = [];

        try
        {
            string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, MunicipalityGeoJsonFilePath);

            if (!System.IO.File.Exists(fullGeoJsonPath))
            {
                logger.LogError("Error: GeoJSON file not found at {FilePath}. Make sure it's copied to output directory.", fullGeoJsonPath);
                return;
            }

            string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
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
            logger.LogError("Error reading or parsing GeoJSON file for municipalities: {Message}", ex.Message);
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

            var municipality = new Municipality
            {
                Name = feature.Attributes["ADM3_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM3_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
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
            await dbContext.Municipalities.AddRangeAsync(municipalitiesToAdd);
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
    /// Placeholder for seeding Barangay entities.
    /// This method will be expanded once GeoJSON format for Barangays is provided.
    /// </summary>
    private static async Task SeedBarangaysAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Barangays.AnyAsync())
        {
            logger.LogInformation("Database already contains barangays. Skipping barangay seeding.");
            return;
        }
        logger.LogInformation("Starting barangays seeding from {FilePath}...", BarangayGeoJsonFilePath);

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, BarangayGeoJsonFilePath);

            if (!System.IO.File.Exists(fullGeoJsonPath))
            {
                logger.LogError("Error: GeoJSON file not found at {Path}. Make sure it's copied to output directory.", fullGeoJsonPath);
                return;
            }

            string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
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
            logger.LogInformation("Error reading or parsing GeoJSON file for provinces: {Message}", ex.Message);
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
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
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
            await dbContext.Barangays.AddRangeAsync(barangaysToAdd);
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
}
