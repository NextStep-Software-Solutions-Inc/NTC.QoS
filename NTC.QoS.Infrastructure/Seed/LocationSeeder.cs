using Microsoft.EntityFrameworkCore;
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
    public static async Task SeedAllAsync(ApplicationDbContext context) // Changed to ApplicationDbContext
    {
        Console.WriteLine("Starting full database seeding process...");

        await SeedRegionsAsync(context);
        await SeedProvincesAsync(context);
        //await SeedMunicipalitiesAsync(context);
        //await SeedBarangaysAsync(context);

        Console.WriteLine("Full database seeding process completed.");
    }

    /// <summary>
    /// Seeds a specific entity type into the database based on the provided enum.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="entityType">The type of entity to seed.</param>
    public static async Task SeedEntityAsync(ApplicationDbContext context, LocationTypeToSeedEnum entityType) // Changed to ApplicationDbContext
    {
        Console.WriteLine($"Starting seeding for entity type: {entityType}...");

        switch (entityType)
        {
            case LocationTypeToSeedEnum.All:
                await SeedAllAsync(context);
                break;
            case LocationTypeToSeedEnum.Region:
                await SeedRegionsAsync(context);
                break;
            case LocationTypeToSeedEnum.Province:
                await SeedProvincesAsync(context);
                break;
            //case LocationTypeToSeedEnum.Municipality:
            //    await SeedMunicipalitiesAsync(context);
            //    break;
            //case LocationTypeToSeedEnum.Barangay:
            //    await SeedBarangaysAsync(context);
            //    break;
            default:
                Console.WriteLine($"Unknown entity type '{entityType}'. No seeding performed.");
                break;
        }

        Console.WriteLine($"Seeding for entity type '{entityType}' completed.");
    }

    /// <summary>
    /// Seeds Region entities from the GeoJSON file.
    /// This logic is adapted from the previous RegionSeeder.
    /// </summary>
    public static async Task SeedRegionsAsync(ApplicationDbContext dbContext) // Changed to ApplicationDbContext dbContext
    {
        if (await dbContext.Regions.AnyAsync()) // Use dbContext
        {
            Console.WriteLine("Database already contains regions. Skipping region seeding.");
            return;
        }

        Console.WriteLine($"Starting region seeding from {RegionGeoJsonFilePath}...");

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, RegionGeoJsonFilePath);

            // Ensure the file exists
            if (!System.IO.File.Exists(fullGeoJsonPath))
            {
                Console.WriteLine($"Error: GeoJSON file not found at {fullGeoJsonPath}. Make sure it's copied to output directory.");
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
                Console.WriteLine("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading or parsing GeoJSON file for regions: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return;
        }

        var regionsToAdd = new List<Region>();

        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                Console.WriteLine("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var region = new Region
            {
                Name = feature.Attributes["ADM1_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM1_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
            };

            if (feature.Geometry is MultiPolygon geoJsonMultiPolygon)
            {
                region.Boundaries = geoJsonMultiPolygon;
                region.Center = geoJsonMultiPolygon.Centroid;
            }
            else if (feature.Geometry is Polygon geoJsonPolygon)
            {
                region.Boundaries = GeometryHelper.CreateMultiPolygon(geoJsonPolygon);
                region.Center = geoJsonPolygon.Centroid;
            }
            else
            {
                Console.WriteLine($"Terminated '{region.Name}' due to unsupported geometry type: {feature.Geometry.GeometryType}");
                return;
            }

            regionsToAdd.Add(region);
        }

        try
        {
            await dbContext.Regions.AddRangeAsync(regionsToAdd);
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"Successfully seeded {regionsToAdd.Count} regions into the database.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving regions to database: {ex.Message}");
            Console.WriteLine(ex.InnerException?.Message); // Log inner exception for more details
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Placeholder for seeding Province entities.
    /// This method will be expanded once GeoJSON format for Provinces is provided.
    /// </summary>
    private static async Task SeedProvincesAsync(ApplicationDbContext dbContext) // Changed to ApplicationDbContext
    {
        if (await dbContext.Provinces.AnyAsync()) // Assuming you have a DbSet<Province>
        {
            Console.WriteLine("Database already contains provinces. Skipping province seeding.");
            return;
        }
        Console.WriteLine($"Starting provinces seeding from {ProvinceGeoJsonFilePath}...");

        FeatureCollection featureCollection = new FeatureCollection();

        try
        {
            string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, ProvinceGeoJsonFilePath);

            // Ensure the file exists
            if (!System.IO.File.Exists(fullGeoJsonPath))
            {
                Console.WriteLine($"Error: GeoJSON file not found at {fullGeoJsonPath}. Make sure it's copied to output directory.");
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
                Console.WriteLine("No features found in the GeoJSON file. Skipping seeding.");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading or parsing GeoJSON file for provinces: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return;
        }

        var regionMap = await dbContext.Regions.ToDictionaryAsync(r => r.Code, r => r.Id);

        var provincesToAdd = new List<Province>();

        foreach (var feature in featureCollection)
        {
            if (feature?.Geometry == null || feature.Attributes == null)
            {
                Console.WriteLine("Skipping feature due to missing geometry or attributes.");
                continue;
            }

            var province = new Province
            {
                Name = feature.Attributes["ADM2_EN"]?.ToString() ?? string.Empty,
                Description = null,
                Code = feature.Attributes["ADM2_PCODE"]?.ToString() ?? string.Empty,
                Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
            };

            if (feature.Geometry is MultiPolygon geoJsonMultiPolygon)
            {
                province.Boundaries = geoJsonMultiPolygon;
                province.Center = geoJsonMultiPolygon.Centroid;
            }
            else if (feature.Geometry is Polygon geoJsonPolygon)
            {
                province.Boundaries = GeometryHelper.CreateMultiPolygon(geoJsonPolygon);
                province.Center = geoJsonPolygon.Centroid;
            }
            else
            {
                Console.WriteLine($"Terminated '{province.Name}' due to unsupported geometry type: {feature.Geometry.GeometryType}");
                return;
            }

            // Find the corresponding Region using ADM1_PCODE
            var regionCode = feature.Attributes["ADM1_PCODE"]?.ToString();
            if (!string.IsNullOrWhiteSpace(regionCode) && regionMap.TryGetValue(regionCode, out var regionId))
            {
                province.RegionId = regionId;
            }
            else
            {
                Console.WriteLine($"Warning: Could not find Region for province '{province.Name}' with ADM1_PCODE '{regionCode}'");
                continue;
            }

            provincesToAdd.Add(province);
        }

        try
        {
            await dbContext.Provinces.AddRangeAsync(provincesToAdd);
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"Successfully seeded {provincesToAdd.Count} provinces into the database.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving provinces to database: {ex.Message}");
            Console.WriteLine(ex.InnerException?.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Placeholder for seeding Municipality entities.
    /// This method will be expanded once GeoJSON format for Municipalities is provided.
    /// </summary>
    //private static async Task SeedMunicipalitiesAsync(ApplicationDbContext dbContext) // Changed to ApplicationDbContext
    //{
    //    if (await dbContext.Municipalities.AnyAsync())
    //    {
    //        Console.WriteLine("Database already contains municipalities. Skipping municipality seeding.");
    //        return;
    //    }
    //    Console.WriteLine($"Starting municipalities seeding from {MunicipalityGeoJsonFilePath}...");

    //    FeatureCollection featureCollection = new FeatureCollection();

    //    try
    //    {
    //        string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, MunicipalityGeoJsonFilePath);

    //        if (!System.IO.File.Exists(fullGeoJsonPath))
    //        {
    //            Console.WriteLine($"Error: GeoJSON file not found at {fullGeoJsonPath}. Make sure it's copied to output directory.");
    //            return;
    //        }

    //        string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
    //        var serializer = GeoJsonSerializer.Create();

    //        using (var stringReader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(geoJsonText)))
    //        {
    //            featureCollection = serializer.Deserialize<FeatureCollection>(stringReader) ?? new FeatureCollection();
    //        }

    //        if (featureCollection == null || !featureCollection.Any())
    //        {
    //            Console.WriteLine("No features found in the GeoJSON file. Skipping seeding.");
    //            return;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error reading or parsing GeoJSON file for municipalities: {ex.Message}");
    //        Console.WriteLine(ex.StackTrace);
    //        return;
    //    }

    //    var provinceMap = await dbContext.Provinces.ToDictionaryAsync(p => p.Code, p => p.Id);

    //    var municipalitiesToAdd = new List<Municipality>();

    //    foreach (var feature in featureCollection)
    //    {
    //        if (feature?.Geometry == null || feature.Attributes == null)
    //        {
    //            Console.WriteLine("Skipping feature due to missing geometry or attributes.");
    //            continue;
    //        }

    //        var municipality = new Municipality
    //        {
    //            Name = feature.Attributes["ADM3_EN"]?.ToString() ?? string.Empty,
    //            Description = null,
    //            Code = feature.Attributes["ADM3_PCODE"]?.ToString() ?? string.Empty,
    //            Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
    //        };

    //        if (feature.Geometry is MultiPolygon geoJsonMultiPolygon)
    //        {
    //            municipality.Boundaries = geoJsonMultiPolygon;
    //            municipality.Center = geoJsonMultiPolygon.Centroid;
    //        }
    //        else if (feature.Geometry is Polygon geoJsonPolygon)
    //        {
    //            municipality.Boundaries = GeometryHelper.CreateMultiPolygon(geoJsonPolygon);
    //            municipality.Center = geoJsonPolygon.Centroid;
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Terminated '{municipality.Name}' due to unsupported geometry type: {feature.Geometry.GeometryType}");
    //            return;
    //        }

    //        var provinceCode = feature.Attributes["ADM2_PCODE"]?.ToString();
    //        if (!string.IsNullOrWhiteSpace(provinceCode) && provinceMap.TryGetValue(provinceCode, out var provinceId))
    //        {
    //            municipality.ProvinceId = provinceId;
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Warning: Could not find Province for municipality '{municipality.Name}' with ADM2_PCODE '{provinceCode}'");
    //            continue;
    //        }

    //        municipalitiesToAdd.Add(municipality);
    //    }

    //    try
    //    {
    //        await dbContext.Municipalities.AddRangeAsync(municipalitiesToAdd);
    //        await dbContext.SaveChangesAsync();
    //        Console.WriteLine($"Successfully seeded {municipalitiesToAdd.Count} municipalities into the database.");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error saving regions to database: {ex.Message}");
    //        Console.WriteLine(ex.InnerException?.Message);
    //        Console.WriteLine(ex.StackTrace);
    //    }
    //}

    ///// <summary>
    ///// Placeholder for seeding Barangay entities.
    ///// This method will be expanded once GeoJSON format for Barangays is provided.
    ///// </summary>
    //private static async Task SeedBarangaysAsync(ApplicationDbContext dbContext) // Changed to ApplicationDbContext
    //{
    //    if (await dbContext.Barangays.AnyAsync())
    //    {
    //        Console.WriteLine("Database already contains barangays. Skipping barangay seeding.");
    //        return;
    //    }
    //    Console.WriteLine($"Starting barangays seeding from {BarangayGeoJsonFilePath}...");

    //    FeatureCollection featureCollection = new FeatureCollection();

    //    try
    //    {
    //        string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, BarangayGeoJsonFilePath);

    //        if (!System.IO.File.Exists(fullGeoJsonPath))
    //        {
    //            Console.WriteLine($"Error: GeoJSON file not found at {fullGeoJsonPath}. Make sure it's copied to output directory.");
    //            return;
    //        }

    //        string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
    //        var serializer = GeoJsonSerializer.Create();

    //        using (var stringReader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(geoJsonText)))
    //        {
    //            featureCollection = serializer.Deserialize<FeatureCollection>(stringReader) ?? new FeatureCollection();
    //        }

    //        if (featureCollection == null || !featureCollection.Any())
    //        {
    //            Console.WriteLine("No features found in the GeoJSON file. Skipping seeding.");
    //            return;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error reading or parsing GeoJSON file for provinces: {ex.Message}");
    //        Console.WriteLine(ex.StackTrace);
    //        return;
    //    }

    //    var municipalityMap = await dbContext.Municipalities.ToDictionaryAsync(m => m.Code, m => m.Id);

    //    var barangaysToAdd = new List<Barangay>();

    //    foreach (var feature in featureCollection)
    //    {
    //        if (feature?.Geometry == null || feature.Attributes == null)
    //        {
    //            Console.WriteLine("Skipping feature due to missing geometry or attributes.");
    //            continue;
    //        }

    //        var barangay = new Barangay
    //        {
    //            Name = feature.Attributes["ADM4_EN"]?.ToString() ?? string.Empty,
    //            Description = null,
    //            Code = feature.Attributes["ADM4_PCODE"]?.ToString() ?? string.Empty,
    //            Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
    //        };

    //        if (feature.Geometry is MultiPolygon geoJsonMultiPolygon)
    //        {
    //            barangay.Boundaries = geoJsonMultiPolygon;
    //            barangay.Center = geoJsonMultiPolygon.Centroid;
    //        }
    //        else if (feature.Geometry is Polygon geoJsonPolygon)
    //        {
    //            barangay.Boundaries = GeometryHelper.CreateMultiPolygon(geoJsonPolygon);
    //            barangay.Center = geoJsonPolygon.Centroid;
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Terminated '{barangay.Name}' due to unsupported geometry type: {feature.Geometry.GeometryType}");
    //            return;
    //        }

    //        var municipalityCode = feature.Attributes["ADM3_PCODE"]?.ToString();
    //        if (!string.IsNullOrWhiteSpace(municipalityCode) && municipalityMap.TryGetValue(municipalityCode, out var municipalityId))
    //        {
    //            barangay.MunicipalityId = municipalityId;
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Warning: Could not find Municipality for barangay '{barangay.Name}' with ADM3_PCODE '{municipalityCode}'");
    //            continue;
    //        }
    //        barangaysToAdd.Add(barangay);
    //    }

    //    try
    //    {
    //        await dbContext.Barangays.AddRangeAsync(barangaysToAdd);
    //        await dbContext.SaveChangesAsync();
    //        Console.WriteLine($"Successfully seeded {barangaysToAdd.Count} barangays into the database.");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error saving regions to database: {ex.Message}");
    //        Console.WriteLine(ex.InnerException?.Message);
    //        Console.WriteLine(ex.StackTrace);
    //    }
    //}
}
