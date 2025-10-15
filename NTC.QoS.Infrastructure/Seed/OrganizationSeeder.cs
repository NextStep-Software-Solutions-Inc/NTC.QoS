using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class OrganizationSeeder
{

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

        var organizations = new[]
        {
            new Organization
            {
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
                Name = "Public",
                Description = "Public organization for general access",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            }
        };


        var baseNames = organizations.Select(o => o.Name).ToList();
        var existingOrgs = await context.Organizations.AsNoTracking().Where(o => baseNames.Contains(o.Name)).ToListAsync();
        var newOrgs = organizations.Where(o => !existingOrgs.Any(eo => eo.Name == o.Name)).ToList();

        if (newOrgs.Count == 0)
        {
            logger.LogInformation("No new organizations to seed. All base organizations already exist.");
            return;
        }

        logger.LogInformation("Creating hardcoded organizations...");

        try
        {
            await context.BulkInsertAsync(newOrgs);
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

