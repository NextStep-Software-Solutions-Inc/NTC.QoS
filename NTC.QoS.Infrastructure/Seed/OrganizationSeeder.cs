using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class OrganizationSeeder
{
    // Fixed UUIDs for consistent organization seeding
    public static readonly Guid NationalOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid RegionalOrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid PublicOrganizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Seeds organizations into the database with hardcoded data and fixed UUIDs.
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting Organization seeding process...");

        await SeedOrganizationsAsync(context, logger);

        logger.LogInformation("Organization seeding process completed.");
    }

    // Seeds organizations into the database from hardcoded array.
    private static async Task SeedOrganizationsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Checking if organizations need to be seeded...");

        // Check if base names already exist (using Name property instead of Key since Organization doesn't have Key)
        var baseNames = new[] { "National", "Regional", "Public" };
        var baseNamesExist = await context.Organizations.AsNoTracking().AnyAsync(o => baseNames.Contains(o.Name));

        if (baseNamesExist)
        {
            logger.LogInformation("Base organization names already exist in database. Skipping organization seeding.");
            return;
        }

        logger.LogInformation("Creating hardcoded organizations...");

        try
        {
            // Simple array to hold the organization data
            var organizations = new[]
            {
                new Organization
                {
                    Id = NationalOrganizationId,
                    Name = "National",
                    Description = "National level organization",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    IsDeleted = false
                },
                new Organization
                {
                    Id = RegionalOrganizationId,
                    Name = "Regional",
                    Description = "Regional level organization",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    IsDeleted = false
                },
                new Organization
                {
                    Id = PublicOrganizationId,
                    Name = "Public",
                    Description = "Public organization for general access",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    IsDeleted = false
                }
            };

            await context.Organizations.AddRangeAsync(organizations);
            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {Count} organizations into the database.", organizations.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during organization seeding: {Message}", ex.Message);
            throw;
        }
    }
}

