using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.Seed;

public static class OrganizationSeeder
{
    // Fixed UUIDs for consistent organization seeding
    public static readonly Guid NationalOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid RegionalOrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

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

        // Check if organizations already exist
        var existingCount = await context.Organizations.AsNoTracking().CountAsync();
        if (existingCount > 0)
        {
            logger.LogInformation("Organizations already exist in database ({Count} organizations found). Skipping organization seeding.", existingCount);
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
                }
            };

            await SeederUtility.BulkInsertWithRetry(context, organizations.ToList(), "organizations", logger);
            logger.LogInformation("Successfully seeded {Count} organizations into the database.", organizations.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during organization seeding: {Message}", ex.Message);
            throw;
        }
    }
}
