using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class LocationSeeder
{
    private const string RegionGeoJsonFilePath = "Data/admin1.geojson";
    private const string ProvinceGeoJsonFilePath = "Data/admin2.geojson";
    private const string MunicipalityGeoJsonFilePath = "Data/admin3.geojson";
    private const string BarangayGeoJsonFilePath = "Data/admin4.geojson";

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

        var featureCollection = await LoadGeoJsonFeatures(RegionGeoJsonFilePath, logger);
        if (featureCollection == null) return;

        var regionsToAdd = new List<Region>();
        
        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogWarning("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var region = new Region
            {
                Name = feature.Attributes["ADM1_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM1_PCODE"]?.ToString() ?? string.Empty
            };

            // FIXED: Preserve exact boundaries without modification
            switch (feature.Geometry)
            {
                case MultiPolygon multiPolygon:
                    region.Boundaries = multiPolygon; // Direct assignment - preserves exact coordinates
                    region.Center = multiPolygon.Centroid;
                    break;
                case Polygon polygon:
                    // Convert to MultiPolygon while preserving exact coordinates
                    var factory = polygon.Factory;
                    region.Boundaries = factory.CreateMultiPolygon(new[] { polygon });
                    region.Center = polygon.Centroid;
                    break;
                default:
                    logger.LogWarning(
                        "Skipping '{Name}' due to unsupported geometry type: {GeometryType}",
                        region.Name,
                        feature.Geometry.GeometryType
                    );
                    continue;
            }

            regionsToAdd.Add(region);
        }

        await BulkInsertWithRetry(dbContext, regionsToAdd, "regions", logger);
        logger.LogInformation("Region seeding completed with {Count} regions added.", regionsToAdd.Count);
    }

    private static async Task SeedProvincesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Provinces.AnyAsync())
        {
            logger.LogInformation("Database already contains provinces. Skipping province seeding.");
            return;
        }

        logger.LogInformation("Starting province seeding from {FilePath}...", ProvinceGeoJsonFilePath);

        var featureCollection = await LoadGeoJsonFeatures(ProvinceGeoJsonFilePath, logger);
        if (featureCollection == null) return;

        // Preload region mapping for performance
        var regionMap = await dbContext.Regions
            .AsNoTracking()
            .Select(r => new { r.Code, r.Id })
            .ToDictionaryAsync(r => r.Code, r => r.Id);

        var provincesToAdd = new List<Province>();

        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogWarning("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var province = new Province
            {
                Name = feature.Attributes["ADM2_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM2_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
            };

            // FIXED: Preserve exact boundaries
            switch (feature.Geometry)
            {
                case MultiPolygon multiPolygon:
                    province.Boundaries = multiPolygon;
                    province.Center = multiPolygon.Centroid;
                    break;
                case Polygon polygon:
                    var factory = polygon.Factory;
                    province.Boundaries = factory.CreateMultiPolygon(new[] { polygon });
                    province.Center = polygon.Centroid;
                    break;
                default:
                    logger.LogWarning(
                        "Skipping '{Name}' due to unsupported geometry type: {GeometryType}",
                        province.Name,
                        feature.Geometry.GeometryType
                    );
                    continue;
            }

            // Link to parent region
            var regionCode = feature.Attributes["ADM1_PCODE"]?.ToString();
            if (!string.IsNullOrWhiteSpace(regionCode) && regionMap.TryGetValue(regionCode, out var regionId))
            {
                province.RegionId = regionId;
            }
            else
            {
                logger.LogWarning(
                    "Could not find Region for province '{Name}' with ADM1_PCODE '{Code}'. Skipping.",
                    province.Name,
                    regionCode
                );
                continue;
            }

            provincesToAdd.Add(province);
        }

        await BulkInsertWithRetry(dbContext, provincesToAdd, "provinces", logger);
        logger.LogInformation("Province seeding completed with {Count} provinces added.", provincesToAdd.Count);
    }

    private static async Task SeedMunicipalitiesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Municipalities.AnyAsync())
        {
            logger.LogInformation("Database already contains municipalities. Skipping municipality seeding.");
            return;
        }

        logger.LogInformation("Starting municipality seeding from {FilePath}...", MunicipalityGeoJsonFilePath);

        var featureCollection = await LoadGeoJsonFeatures(MunicipalityGeoJsonFilePath, logger);
        if (featureCollection == null) return;

        // Preload province mapping for performance
        var provinceMap = await dbContext.Provinces
            .AsNoTracking()
            .Select(p => new { p.Code, p.Id })
            .ToDictionaryAsync(p => p.Code, p => p.Id);

        var municipalitiesToAdd = new List<Municipality>();

        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogWarning("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var municipality = new Municipality
            {
                Name = feature.Attributes["ADM3_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM3_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
            };

            // FIXED: Preserve exact boundaries
            switch (feature.Geometry)
            {
                case MultiPolygon multiPolygon:
                    municipality.Boundaries = multiPolygon;
                    municipality.Center = multiPolygon.Centroid;
                    break;
                case Polygon polygon:
                    var factory = polygon.Factory;
                    municipality.Boundaries = factory.CreateMultiPolygon(new[] { polygon });
                    municipality.Center = polygon.Centroid;
                    break;
                default:
                    logger.LogWarning(
                        "Skipping '{Name}' due to unsupported geometry type: {GeometryType}",
                        municipality.Name,
                        feature.Geometry.GeometryType
                    );
                    continue;
            }

            // Link to parent province
            var provinceCode = feature.Attributes["ADM2_PCODE"]?.ToString();
            if (!string.IsNullOrWhiteSpace(provinceCode) && provinceMap.TryGetValue(provinceCode, out var provinceId))
            {
                municipality.ProvinceId = provinceId;
            }
            else
            {
                logger.LogWarning(
                    "Could not find Province for municipality '{Name}' with ADM2_PCODE '{Code}'. Skipping.",
                    municipality.Name,
                    provinceCode
                );
                continue;
            }

            municipalitiesToAdd.Add(municipality);
        }

        await BulkInsertWithRetry(dbContext, municipalitiesToAdd, "municipalities", logger);
        logger.LogInformation("Municipality seeding completed with {Count} municipalities added.", municipalitiesToAdd.Count);
    }

    private static async Task SeedBarangaysAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Barangays.AnyAsync())
        {
            logger.LogInformation("Database already contains barangays. Skipping barangay seeding.");
            return;
        }

        logger.LogInformation("Starting barangay seeding from {FilePath}...", BarangayGeoJsonFilePath);

        var featureCollection = await LoadGeoJsonFeatures(BarangayGeoJsonFilePath, logger);
        if (featureCollection == null) return;

        // Preload municipality mapping for performance
        var municipalityMap = await dbContext.Municipalities
            .AsNoTracking()
            .Select(m => new { m.Code, m.Id })
            .ToDictionaryAsync(m => m.Code, m => m.Id);

        var barangaysToAdd = new List<Barangay>();

        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                logger.LogWarning("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var barangay = new Barangay
            {
                Name = feature.Attributes["ADM4_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM4_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
            };

            // FIXED: Preserve exact boundaries
            switch (feature.Geometry)
            {
                case MultiPolygon multiPolygon:
                    barangay.Boundaries = multiPolygon;
                    barangay.Center = multiPolygon.Centroid;
                    break;
                case Polygon polygon:
                    var factory = polygon.Factory;
                    barangay.Boundaries = factory.CreateMultiPolygon(new[] { polygon });
                    barangay.Center = polygon.Centroid;
                    break;
                default:
                    logger.LogWarning(
                        "Skipping '{Name}' due to unsupported geometry type: {GeometryType}",
                        barangay.Name,
                        feature.Geometry.GeometryType
                    );
                    continue;
            }

            // Link to parent municipality
            var municipalityCode = feature.Attributes["ADM3_PCODE"]?.ToString();
            if (!string.IsNullOrWhiteSpace(municipalityCode) && municipalityMap.TryGetValue(municipalityCode, out var municipalityId))
            {
                barangay.MunicipalityId = municipalityId;
            }
            else
            {
                logger.LogWarning(
                    "Could not find Municipality for barangay '{Name}' with ADM3_PCODE '{Code}'. Skipping.",
                    barangay.Name,
                    municipalityCode
                );
                continue;
            }

            barangaysToAdd.Add(barangay);
        }

        await BulkInsertWithRetry(dbContext, barangaysToAdd, "barangays", logger);
        logger.LogInformation("Barangay seeding completed with {Count} barangays added.", barangaysToAdd.Count);
    }

    // HELPER METHODS FOR OPTIMIZATION

    /// <summary>
    /// Optimized GeoJSON loading with proper error handling
    /// </summary>
    private static async Task<FeatureCollection?> LoadGeoJsonFeatures(string fileName, ILogger logger)
    {
        try
        {
            // Try multiple potential paths for the GeoJSON files
            var potentialPaths = new[]
            {
                Path.Combine(Environment.CurrentDirectory, fileName),
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                // Also try without the Data/ prefix in case the files are copied to the root
                Path.Combine(Environment.CurrentDirectory, Path.GetFileName(fileName)),
                Path.Combine(AppContext.BaseDirectory, Path.GetFileName(fileName))
            };

            string? fullGeoJsonPath = null;
            foreach (var path in potentialPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    fullGeoJsonPath = path;
                    break;
                }
            }

            if (string.IsNullOrEmpty(fullGeoJsonPath))
            {
                logger.LogError("GeoJSON file not found. Searched paths: {SearchedPaths}. Make sure the file {FileName} is copied to output directory.", 
                    string.Join(", ", potentialPaths), fileName);
                logger.LogInformation("Current working directory: {CurrentDirectory}", Directory.GetCurrentDirectory());
                logger.LogInformation("Base directory: {BaseDirectory}", AppContext.BaseDirectory);
                return null;
            }

            logger.LogInformation("Loading GeoJSON from {FilePath}...", fullGeoJsonPath);

            string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
            var serializer = GeoJsonSerializer.Create();

            using var stringReader = new Newtonsoft.Json.JsonTextReader(new StringReader(geoJsonText));
            var featureCollection = serializer.Deserialize<FeatureCollection>(stringReader) ?? new FeatureCollection();

            if (!featureCollection.Any())
            {
                logger.LogWarning("No features found in {FilePath}. Skipping seeding.", fileName);
                return null;
            }

            logger.LogInformation("Successfully loaded {Count} features from {FilePath}", featureCollection.Count, fullGeoJsonPath);
            return featureCollection;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading or parsing GeoJSON file {FilePath}: {ErrorMessage}", fileName, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Optimized bulk insert with retry logic and better error handling
    /// </summary>
    private static async Task BulkInsertWithRetry<T>(ApplicationDbContext dbContext, List<T> entities, string entityType, ILogger logger) where T : class
    {
        if (!entities.Any())
        {
            logger.LogWarning("No {EntityType} to insert.", entityType);
            return;
        }

        const int maxRetries = 3;
        const int batchSize = 1000; // Process in smaller batches for better memory management

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                logger.LogInformation("Inserting {Count} {EntityType} (attempt {Attempt}/{MaxAttempts})...", 
                    entities.Count, entityType, retry + 1, maxRetries);

                // Process in batches for better performance with large datasets
                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    await dbContext.BulkInsertAsync(batch);
                    
                    if (batch.Count == batchSize) // Only log for full batches to reduce noise
                    {
                        logger.LogInformation("Inserted batch {BatchStart}-{BatchEnd} of {Total} {EntityType}", 
                            i + 1, i + batch.Count, entities.Count, entityType);
                    }
                }

                await dbContext.SaveChangesAsync();
                logger.LogInformation("Successfully seeded {Count} {EntityType} into the database.", entities.Count, entityType);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving {EntityType} to database (attempt {Attempt}/{MaxAttempts}): {Message}", 
                    entityType, retry + 1, maxRetries, ex.Message);
                
                if (retry == maxRetries - 1)
                {
                    throw;
                }
                
                await Task.Delay(1000); // Wait 1 second before retry
            }
        }
        
        throw new InvalidOperationException($"Failed to save {entityType} after {maxRetries} attempts.");
    }
}
