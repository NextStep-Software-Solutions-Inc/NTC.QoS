using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class RoleSeeder
{
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
        var roles = new[]
            {
                new Role
                {
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
        

        logger.LogInformation("Creating missing roles if they do not exist...");
        
        try
        {
            var baseKeys = roles.Select(r => r.Key).ToList();
            var newRoles = new List<Role>();
            // Check if base keys already exist
            var existingRoles = await context.Roles
            .AsNoTracking()
            .Where(r => baseKeys.Contains(r.Key))
            .Select(r => r.Key)
            .ToListAsync();

            foreach (var role in roles)
            {
                if (!existingRoles.Contains(role.Key)) newRoles.Add(role);
            }

            await context.BulkInsertAsync(newRoles);
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
