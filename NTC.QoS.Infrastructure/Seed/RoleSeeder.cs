using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class RoleSeeder
{
    // Fixed UUIDs for consistent role seeding
    public static readonly Guid AdminRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid SuperAdminRoleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid FieldOfficerRoleId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid UserRoleId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    // Seeds roles into the database with hardcoded data and fixed UUIDs.
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting Role seeding process...");

        await SeedRolesAsync(context, logger);

        logger.LogInformation("Role seeding process completed.");
    }

    // Seeds roles into the database from hardcoded array.
    private static async Task SeedRolesAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Checking if roles need to be seeded...");

        // Check if base keys already exist
        var baseKeys = new[] { "admin", "super_admin", "field_officer", "user" };
        var baseKeysExist = await context.Roles.AsNoTracking().AnyAsync(r => baseKeys.Contains(r.Key));

        if (baseKeysExist)
        {
            logger.LogInformation("Base role keys already exist in database. Skipping role seeding.");
            return;
        }

        logger.LogInformation("Creating hardcoded roles...");

        try
        {
            // Simple array to hold the role data
            var roles = new[]
            {
                new Role
                {
                    Id = AdminRoleId,
                    Name = "Admin User",
                    Key = "admin",
                    Description = "Administrative user with elevated privileges",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    IsDeleted = false
                },
                new Role
                {
                    Id = SuperAdminRoleId,
                    Name = "SuperAdmin",
                    Key = "super_admin",
                    Description = "Super administrator with full system access",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    IsDeleted = false
                },
                new Role
                {
                    Id = FieldOfficerRoleId,
                    Name = "Field Officer",
                    Key = "field_officer",
                    Description = "Field officer with limited operational access",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    IsDeleted = false
                },
                new Role
                {
                    Id = UserRoleId,
                    Name = "User",
                    Key = "user",
                    Description = "Standard user with basic access",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    IsDeleted = false
                }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {Count} roles into the database.", roles.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during role seeding: {Message}", ex.Message);
            throw;
        }
    }
}
