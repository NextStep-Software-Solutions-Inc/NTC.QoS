using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore; // Required for DbContext
using NetTopologySuite.Features; // Required for FeatureCollection
using NetTopologySuite.Geometries; // Required for MultiPolygon, Point
using NetTopologySuite.IO; // Required for GeoJsonSerializer
using NTC.QoS.Domain.Entities; // Adjust namespace to where your entities are located
using NTC.QoS.Domain.Enums; // Required for Assembly
using NTC.QoS.Domain.Helper; // Required for Assembly
using NTC.QoS.Infrastructure.Persistence; // Assuming ApplicationDbContext is here
using NTC.QoS.Infrastructure.Seeder;

namespace NTC.QoS.Infrastructure.Seeder; // Adjust namespace as per your project structure

public static class DatabaseSeeder
{
    private const string RegionGeoJsonFilePath = "Data/admin1.geojson";
    private const string ProvinceGeoJsonFilePath = "Data/admin2.geojson";
    private const string MunicipalityGeoJsonFilePath = "Data/admin3.geojson";
    private const string BarangayGeoJsonFilePath = "Data/admin4.geojson";

    /// <summary>
    /// Seeds all configured entities into the database.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    public static async Task SeedAllAsync(ApplicationDbContext context) // Changed to ApplicationDbContext
    {
        Console.WriteLine("Starting full database seeding process...");

        await SeedRegionsAsync(context);
        await SeedProvincesAsync(context);
        await SeedMunicipalitiesAsync(context);
        await SeedBarangaysAsync(context);

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
            case LocationTypeToSeedEnum.Municipality:
                await SeedMunicipalitiesAsync(context);
                break;
            case LocationTypeToSeedEnum.Barangay:
                await SeedBarangaysAsync(context);
                break;
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
    public static async Task SeedRegionsAsync(ApplicationDbContext dbContext) 
    {
        if (await dbContext.Regions.AnyAsync())
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
                region.Boundaries = GeometryHelper.CreateMultiPolygon(geoJsonPolygon)!;
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
            Console.WriteLine(ex.InnerException?.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Placeholder for seeding Province entities.
    /// This method will be expanded once GeoJSON format for Provinces is provided.
    /// </summary>
    private static async Task SeedProvincesAsync(ApplicationDbContext dbContext)
    {
        await ProvinceSeeder.SeedProvincesAsync(dbContext);

        //if (await dbContext.Provinces.AnyAsync())
        //{
        //    Console.WriteLine("Database already contains provinces. Skipping province seeding.");
        //    return;
        //}

        //Console.WriteLine($"Starting province seeding from {ProvinceGeoJsonFilePath}...");

        //FeatureCollection featureCollection = new FeatureCollection();

        //try
        //{
        //    string fullGeoJsonPath = Path.Combine(Environment.CurrentDirectory, ProvinceGeoJsonFilePath);

        //    // Ensure the file exists
        //    if (!System.IO.File.Exists(fullGeoJsonPath))
        //    {
        //        Console.WriteLine($"Error: GeoJSON file not found at {fullGeoJsonPath}. Make sure it's copied to output directory.");
        //        return;
        //    }

        //    string geoJsonText = await System.IO.File.ReadAllTextAsync(fullGeoJsonPath);
        //    var serializer = GeoJsonSerializer.Create();

        //    using (var stringReader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(geoJsonText)))
        //    {
        //        featureCollection = serializer.Deserialize<FeatureCollection>(stringReader) ?? new FeatureCollection();
        //    }

        //    if (featureCollection == null || !featureCollection.Any())
        //    {
        //        Console.WriteLine("No features found in the GeoJSON file. Skipping seeding.");
        //        return;
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"Error reading or parsing GeoJSON file for regions: {ex.Message}");
        //    Console.WriteLine(ex.StackTrace);
        //    return;
        //}

        //var provincesToAdd = new List<Province>();

        //foreach (var feature in featureCollection)
        //{
        //    if (feature?.Geometry == null || feature.Attributes == null)
        //    {
        //        Console.WriteLine("Skipping feature due to missing geometry or attributes.");
        //        continue;
        //    }

        //    var province = new Province
        //    {
        //        Name = feature.Attributes["ADM2_EN"]?.ToString() ?? string.Empty,
        //        Description = null,
        //        Code = feature.Attributes["ADM2_PCODE"]?.ToString() ?? string.Empty,
        //        Area = float.TryParse(feature.Attributes["AREA_SQKM"]?.ToString(), out float areaValue) ? areaValue : 0f
        //    };

        //    if (feature.Geometry is MultiPolygon geoJsonMultiPolygon)
        //    {
        //        province.Boundaries = geoJsonMultiPolygon;
        //        province.Center = geoJsonMultiPolygon.Centroid;
        //    }
        //    else if (feature.Geometry is Polygon geoJsonPolygon)
        //    {
        //        province.Boundaries = GeometryHelper.CreateMultiPolygon(geoJsonPolygon)!;
        //        province.Center = geoJsonPolygon.Centroid;
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Terminated '{province.Name}' due to unsupported geometry type: {feature.Geometry.GeometryType}");
        //        return;
        //    }

        //    provincesToAdd.Add(province);
        //}

        //try
        //{
        //    await dbContext.Provinces.AddRangeAsync(provincesToAdd);
        //    await dbContext.SaveChangesAsync();
        //    Console.WriteLine($"Successfully seeded {provincesToAdd.Count} provinces into the database.");
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"Error saving provinces to database: {ex.Message}");
        //    Console.WriteLine(ex.InnerException?.Message);
        //    Console.WriteLine(ex.StackTrace);
        //}
    }

    /// <summary>
    /// Placeholder for seeding Municipality entities.
    /// This method will be expanded once GeoJSON format for Municipalities is provided.
    /// </summary>
    private static async Task SeedMunicipalitiesAsync(ApplicationDbContext context) // Changed to ApplicationDbContext
    {
        if (await context.Municipalities.AnyAsync()) // Assuming you have a DbSet<Municipality>
        {
            Console.WriteLine("Database already contains municipalities. Skipping municipality seeding.");
            return;
        }
        Console.WriteLine("Seeding Municipalities (Placeholder - implement actual logic when GeoJSON is available)...");
    }

    /// <summary>
    /// Placeholder for seeding Barangay entities.
    /// This method will be expanded once GeoJSON format for Barangays is provided.
    /// </summary>
    private static async Task SeedBarangaysAsync(ApplicationDbContext context) // Changed to ApplicationDbContext
    {
        if (await context.Barangays.AnyAsync()) // Assuming you have a DbSet<Barangay>
        {
            Console.WriteLine("Database already contains barangays. Skipping barangay seeding.");
            return;
        }
        Console.WriteLine("Seeding Barangays (Placeholder - implement actual logic when GeoJSON is available)...");
    }
}
