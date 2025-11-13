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

        // Seed Regions first (no dependencies)
        await SeedRegionsAsync(context, blobStorageService, logger, forceReseed);
        
        // Reload region map AFTER region seeding to get updated/new IDs
        logger.LogInformation("Loading region map for province seeding...");
        var regionMap = await context.Regions.AsNoTracking()
            .Select(r => new { r.Code, r.Id })
            .ToDictionaryAsync(r => r.Code, r => r.Id);
        logger.LogInformation("Loaded {Count} regions for province seeding.", regionMap.Count);
        
        // Seed Provinces with region map
        await SeedProvincesAsync(context, blobStorageService, logger, regionMap, forceReseed);
        
        // Reload province map AFTER province seeding
        logger.LogInformation("Loading province map for municipality seeding...");
        var provinceMap = await context.Provinces.AsNoTracking()
            .Select(p => new { p.Code, p.Id })
            .ToDictionaryAsync(p => p.Code, p => p.Id);
        logger.LogInformation("Loaded {Count} provinces for municipality seeding.", provinceMap.Count);
        
        // Seed Municipalities with province map
        await SeedMunicipalitiesAsync(context, blobStorageService, logger, provinceMap, forceReseed);
        
        // Reload municipality map AFTER municipality seeding
        logger.LogInformation("Loading municipality map for barangay seeding...");
        var municipalityMap = await context.Municipalities.AsNoTracking()
            .Select(m => new { m.Code, m.Id })
            .ToDictionaryAsync(m => m.Code, m => m.Id);
        logger.LogInformation("Loaded {Count} municipalities for barangay seeding.", municipalityMap.Count);
        
        // Seed Barangays with municipality map
        await SeedBarangaysAsync(context, blobStorageService, logger, municipalityMap, forceReseed);

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

            var hasData = regionCount > 0 && provinceCount > 0 && municipalityCount > 0 && barangayCount > 0;

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

        // Load existing regions with IDs for proper update matching
        var existingRegions = await dbContext.Regions.AsNoTracking()
            .Select(r => new { r.Id, r.Code })
            .ToDictionaryAsync(r => r.Code, r => r.Id);

        var now = DateTime.UtcNow;
        var userId = "System"; // Seeding is always done by System

        // Parse all regions from GeoJSON
        var allRegionsFromGeoJson = featureCollection
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
            .Where(region => !string.IsNullOrWhiteSpace(region.code))
            .ToList();

        if (allRegionsFromGeoJson.Count == 0)
        {
            logger.LogInformation("No valid regions found in GeoJSON file.");
            return;
        }

        // Separate into new and existing
        var regionsToInsert = new List<Region>();
        var regionsToUpdate = new List<Region>();

        foreach (var region in allRegionsFromGeoJson)
        {
            var regionEntity = new Region
            {
                Name = region.name ?? string.Empty,
                Description = null,
                Code = region.code ?? string.Empty,
                Area = region.area,
                TotalPopulation = region.totalPopulation,
                HouseholdPopulation = region.householdPopulation,
                HouseholdCount = region.householdCount,
                Boundaries = region.boundaries,
                Center = region.boundaries.Centroid,
                CreatedAt = now,
                CreatedBy = userId,
                UpdatedAt = now,
                UpdatedBy = userId
            };

            // OPTIMIZED: Use TryGetValue instead of ContainsKey + indexer
            if (existingRegions.TryGetValue(region.code!, out var existingId))
            {
                if (forceReseed)
                {
                    // Set the ID from existing entity for proper update
                    regionEntity.Id = existingId;
                    regionsToUpdate.Add(regionEntity);
                }
                // else: existing record, skip (not forcing reseed)
            }
            else
            {
                regionsToInsert.Add(regionEntity);
            }
        }

        // Insert new regions
        if (regionsToInsert.Count > 0)
        {
            await dbContext.BulkInsertAsync(regionsToInsert);
            logger.LogInformation("Inserted {Count} new regions.", regionsToInsert.Count);
        }

        // Update existing regions (only when forceReseed=true)
        if (regionsToUpdate.Count > 0 && forceReseed)
        {
            await dbContext.BulkUpdateAsync(regionsToUpdate, config => 
            {
                config.PropertiesToInclude = new List<string> 
                { 
                    nameof(Region.Name),
                    nameof(Region.Area),
                    nameof(Region.TotalPopulation),
                    nameof(Region.HouseholdPopulation),
                    nameof(Region.HouseholdCount),
                    nameof(Region.Boundaries),
                    nameof(Region.Center),
                    nameof(Region.UpdatedAt),
                    nameof(Region.UpdatedBy)
                };
            });
            logger.LogInformation("Updated {Count} existing regions.", regionsToUpdate.Count);
        }

        if (regionsToInsert.Count == 0 && regionsToUpdate.Count == 0)
        {
            logger.LogInformation("No new regions to seed. All regions from the GeoJSON already exist in the database.");
            return;
        }
        
        logger.LogInformation("Region seeding completed. Inserted: {InsertCount}, Updated: {UpdateCount}", 
            regionsToInsert.Count, regionsToUpdate.Count);
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
    /// <param name="dbContext">The DbContext instance.</param>
    /// <param name="blobStorageService">The blob storage service instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="forceReseed">If true, forces reseeding even if data exists.</param>
    /// <param name="regionMap">Pre-loaded region code to ID mapping for performance and consistency.</param>
    private static async Task SeedProvincesAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger, Dictionary<string, Guid>? regionMap, bool forceReseed = false)
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

        // Use provided regionMap or load it if not provided (backward compatibility)
        if (regionMap == null)
        {
            logger.LogInformation("Region map not provided, loading from database...");
            regionMap = await dbContext.Regions.AsNoTracking()
                .Select(r => new { r.Code, r.Id })
                .ToDictionaryAsync(r => r.Code, r => r.Id);
            logger.LogInformation("Loaded {Count} regions.", regionMap.Count);
        }
        
        // Load existing provinces with IDs for proper update matching
        var existingProvinces = await dbContext.Provinces.AsNoTracking()
            .Select(p => new { p.Id, p.Code })
            .ToDictionaryAsync(p => p.Code, p => p.Id);

        var now = DateTime.UtcNow;
        var userId = "System";
     
        // OPTIMIZED: Parse all provinces from GeoJSON with TryGetValue for regionMap lookup
        var allProvincesFromGeoJson = featureCollection
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
                    !string.IsNullOrWhiteSpace(province.regionCode))
            .Select(province => new
            {
                province.code,
                province.name,
                province.area,
                province.totalPopulation,
                province.householdPopulation,
                province.householdCount,
                province.boundaries,
                province.regionCode,
                // OPTIMIZED: Use TryGetValue to get regionId (null if not found)
                regionId = regionMap.TryGetValue(province.regionCode!, out var rId) ? rId : (Guid?)null
            })
            .Where(province => province.regionId.HasValue)
            .ToList();

        if (allProvincesFromGeoJson.Count == 0)
        {
            logger.LogInformation("No valid provinces found in GeoJSON file or missing parent regions.");
            return;
        }

        // Separate into new and existing
        var provincesToInsert = new List<Province>();
        var provincesToUpdate = new List<Province>();

        foreach (var province in allProvincesFromGeoJson)
        {
            var provinceEntity = new Province
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
                RegionId = province.regionId!.Value,
                CreatedAt = now,
                CreatedBy = userId,
                UpdatedAt = now,
                UpdatedBy = userId
            };

            // OPTIMIZED: Use TryGetValue instead of ContainsKey + indexer
            if (existingProvinces.TryGetValue(province.code!, out var existingId))
            {
                if (forceReseed)
                {
                    // Set the ID from existing entity for proper update
                    provinceEntity.Id = existingId;
                    provincesToUpdate.Add(provinceEntity);
                }
                // else: existing record, skip (not forcing reseed)
            }
            else
            {
                provincesToInsert.Add(provinceEntity);
            }
        }

        // Insert new provinces
        if (provincesToInsert.Count > 0)
        {
            await dbContext.BulkInsertAsync(provincesToInsert);
            logger.LogInformation("Inserted {Count} new provinces.", provincesToInsert.Count);
        }

        // Update existing provinces (only when forceReseed=true)
        if (provincesToUpdate.Count > 0 && forceReseed)
        {
            await dbContext.BulkUpdateAsync(provincesToUpdate, config => 
            {
                config.PropertiesToInclude = new List<string> 
                { 
                    nameof(Province.Name),
                    nameof(Province.Area),
                    nameof(Province.TotalPopulation),
                    nameof(Province.HouseholdPopulation),
                    nameof(Province.HouseholdCount),
                    nameof(Province.Boundaries),
                    nameof(Province.Center),
                    nameof(Province.RegionId),
                    nameof(Province.UpdatedAt),
                    nameof(Province.UpdatedBy)
                };
            });
            logger.LogInformation("Updated {Count} existing provinces.", provincesToUpdate.Count);
        }

        if (provincesToInsert.Count == 0 && provincesToUpdate.Count == 0)
        {
            logger.LogInformation("No new provinces to seed. All provinces from the GeoJSON already exist in the database.");
            return;
        }
        
        logger.LogInformation("Province seeding completed. Inserted: {InsertCount}, Updated: {UpdateCount}", 
            provincesToInsert.Count, provincesToUpdate.Count);
    }

    /// <summary>
    /// Seeds Municipality entities from the GeoJSON file in blob storage.
    /// </summary>
    /// <param name="dbContext">The DbContext instance.</param>
    /// <param name="blobStorageService">The blob storage service instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="forceReseed">If true, forces reseeding even if data exists.</param>
    /// <param name="provinceMap">Pre-loaded province code to ID mapping for performance and consistency.</param>
    private static async Task SeedMunicipalitiesAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger, Dictionary<string, Guid>? provinceMap, bool forceReseed = false)
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

        // Use provided provinceMap or load it if not provided (backward compatibility)
        if (provinceMap == null)
        {
            logger.LogInformation("Province map not provided, loading from database...");
            provinceMap = await dbContext.Provinces.AsNoTracking()
                .Select(p => new { p.Code, p.Id })
                .ToDictionaryAsync(p => p.Code, p => p.Id);
            logger.LogInformation("Loaded {Count} provinces.", provinceMap.Count);
        }
        
        // Load existing municipalities with IDs for proper update matching
        var existingMunicipalities = await dbContext.Municipalities.AsNoTracking()
            .Select(m => new { m.Id, m.Code })
            .ToDictionaryAsync(m => m.Code, m => m.Id);

        var now = DateTime.UtcNow;
        var userId = "System";

        // OPTIMIZED: Parse all municipalities from GeoJSON with TryGetValue for provinceMap lookup
        var allMunicipalitiesFromGeoJson = featureCollection
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
                    !string.IsNullOrWhiteSpace(municipality.provinceCode))
            .Select(municipality => new
            {
                municipality.code,
                municipality.name,
                municipality.area,
                municipality.totalPopulation,
                municipality.householdPopulation,
                municipality.householdCount,
                municipality.boundaries,
                municipality.provinceCode,
                // OPTIMIZED: Use TryGetValue to get provinceId (null if not found)
                provinceId = provinceMap.TryGetValue(municipality.provinceCode!, out var pId) ? pId : (Guid?)null
            })
            .Where(municipality => municipality.provinceId.HasValue)
            .ToList();

        if (allMunicipalitiesFromGeoJson.Count == 0)
        {
            logger.LogInformation("No valid municipalities found in GeoJSON file.");
            return;
        }

        // Separate into new and existing
        var municipalitiesToInsert = new List<Municipality>();
        var municipalitiesToUpdate = new List<Municipality>();

        foreach (var municipality in allMunicipalitiesFromGeoJson)
        {
            var isCity = !string.IsNullOrWhiteSpace(municipality.name) && 
                         (municipality.name.Contains("City", StringComparison.OrdinalIgnoreCase) || 
                          municipality.name.Contains("Capital", StringComparison.OrdinalIgnoreCase));

            var municipalityEntity = new Municipality
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
                ProvinceId = municipality.provinceId!.Value,
                IsCity = isCity,
                CreatedAt = now,
                CreatedBy = userId,
                UpdatedAt = now,
                UpdatedBy = userId
            };

            // OPTIMIZED: Use TryGetValue instead of ContainsKey + indexer
            if (existingMunicipalities.TryGetValue(municipality.code!, out var existingId))
            {
                if (forceReseed)
                {
                    // Set the ID from existing entity for proper update
                    municipalityEntity.Id = existingId;
                    municipalitiesToUpdate.Add(municipalityEntity);
                }
                // else: existing record, skip (not forcing reseed)
            }
            else
            {
                municipalitiesToInsert.Add(municipalityEntity);
            }
        }

        // Insert new municipalities
        if (municipalitiesToInsert.Count > 0)
        {
            await dbContext.BulkInsertAsync(municipalitiesToInsert);
            logger.LogInformation("Inserted {Count} new municipalities.", municipalitiesToInsert.Count);
        }

        // Update existing municipalities (only when forceReseed=true)
        if (municipalitiesToUpdate.Count > 0 && forceReseed)
        {
            await dbContext.BulkUpdateAsync(municipalitiesToUpdate, config => 
            {
                config.PropertiesToInclude = new List<string> 
                { 
                    nameof(Municipality.IsCity),
                    nameof(Municipality.Name),
                    nameof(Municipality.Area),
                    nameof(Municipality.TotalPopulation),
                    nameof(Municipality.HouseholdPopulation),
                    nameof(Municipality.HouseholdCount),
                    nameof(Municipality.Boundaries),
                    nameof(Municipality.Center),
                    nameof(Municipality.ProvinceId),
                    nameof(Municipality.UpdatedAt),
                    nameof(Municipality.UpdatedBy)
                };
            });
            logger.LogInformation("Updated {Count} existing municipalities with IsCity and other properties.", municipalitiesToUpdate.Count);
        }

        if (municipalitiesToInsert.Count == 0 && municipalitiesToUpdate.Count == 0)
        {
            logger.LogInformation("No new municipalities to seed. All municipalities from the GeoJSON already exist in the database.");
            return;
        }
        
        logger.LogInformation("Municipality seeding completed. Inserted: {InsertCount}, Updated: {UpdateCount}", 
            municipalitiesToInsert.Count, municipalitiesToUpdate.Count);
    }

    /// <summary>
    /// Seeds Barangay entities from the GeoJSON file in blob storage.
    /// </summary>
    /// <param name="dbContext">The DbContext instance.</param>
    /// <param name="blobStorageService">The blob storage service instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="forceReseed">If true, forces reseeding even if data exists.</param>
    /// <param name="municipalityMap">Pre-loaded municipality code to ID mapping for performance and consistency.</param>
    private static async Task SeedBarangaysAsync(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, ILogger logger, Dictionary<string, Guid>? municipalityMap, bool forceReseed = false)
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

        // Use provided municipalityMap or load it if not provided (backward compatibility)
        if (municipalityMap == null)
        {
            logger.LogInformation("Municipality map not provided, loading from database...");
            municipalityMap = await dbContext.Municipalities.AsNoTracking()
                .Select(m => new { m.Code, m.Id })
                .ToDictionaryAsync(m => m.Code, m => m.Id);
            logger.LogInformation("Loaded {Count} municipalities.", municipalityMap.Count);
        }
        
        // Load existing barangays with IDs for proper update matching
        var existingBarangays = await dbContext.Barangays.AsNoTracking()
            .Select(b => new { b.Id, b.Code })
            .ToDictionaryAsync(b => b.Code, b => b.Id);

        var now = DateTime.UtcNow;
        var userId = "System";

        // OPTIMIZED: Parse all barangays from GeoJSON with TryGetValue for municipalityMap lookup
        var allBarangaysFromGeoJson = featureCollection
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
                    !string.IsNullOrWhiteSpace(barangay.municipalityCode))
            .Select(barangay => new
            {
                barangay.code,
                barangay.name,
                barangay.area,
                barangay.boundaries,
                barangay.municipalityCode,
                // OPTIMIZED: Use TryGetValue to get municipalityId (null if not found)
                municipalityId = municipalityMap.TryGetValue(barangay.municipalityCode!, out var mId) ? mId : (Guid?)null
            })
            .Where(barangay => barangay.municipalityId.HasValue)
            .ToList();

        if (allBarangaysFromGeoJson.Count == 0)
        {
            logger.LogInformation("No valid barangays found in GeoJSON file or missing parent municipalities.");
            return;
        }

        // Separate into new and existing
        var barangaysToInsert = new List<Barangay>();
        var barangaysToUpdate = new List<Barangay>();

        foreach (var barangay in allBarangaysFromGeoJson)
        {
            var barangayEntity = new Barangay
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
                MunicipalityId = barangay.municipalityId!.Value,
                CreatedAt = now,
                CreatedBy = userId,
                UpdatedAt = now,
                UpdatedBy = userId
            };

            // OPTIMIZED: Use TryGetValue instead of ContainsKey + indexer
            if (existingBarangays.TryGetValue(barangay.code!, out var existingId))
            {
                if (forceReseed)
                {
                    // Set the ID from existing entity for proper update
                    barangayEntity.Id = existingId;
                    barangaysToUpdate.Add(barangayEntity);
                }
                // else: existing record, skip (not forcing reseed)
            }
            else
            {
                barangaysToInsert.Add(barangayEntity);
            }
        }

        // Insert new barangays
        if (barangaysToInsert.Count > 0)
        {
            await dbContext.BulkInsertAsync(barangaysToInsert);
            logger.LogInformation("Inserted {Count} new barangays.", barangaysToInsert.Count);
        }

        // Update existing barangays (only when forceReseed=true)
        if (barangaysToUpdate.Count > 0 && forceReseed)
        {
            await dbContext.BulkUpdateAsync(barangaysToUpdate, config => 
            {
                config.PropertiesToInclude = new List<string> 
                { 
                    nameof(Barangay.Name),
                    nameof(Barangay.Area),
                    nameof(Barangay.Boundaries),
                    nameof(Barangay.Center),
                    nameof(Barangay.MunicipalityId),
                    nameof(Barangay.UpdatedAt),
                    nameof(Barangay.UpdatedBy)
                };
            });
            logger.LogInformation("Updated {Count} existing barangays.", barangaysToUpdate.Count);
        }

        if (barangaysToInsert.Count == 0 && barangaysToUpdate.Count == 0)
        {
            logger.LogInformation("No new barangays to seed. All barangays from the GeoJSON already exist in the database.");
            return;
        }
        
        logger.LogInformation("Barangay seeding completed. Inserted: {InsertCount}, Updated: {UpdateCount}", 
            barangaysToInsert.Count, barangaysToUpdate.Count);
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
