using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Utility class for running database seeders independently.
/// Useful for development, testing, and manual seeding operations.
/// </summary>
public static class SeederUtility
{
    /// <summary>
    /// Runs all available seeders for a complete database setup.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public static async Task SeedAllAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting complete database seeding process...");
        
        try
        {
            // Seed core authentication and authorization data first
            await RoleSeeder.SeedAsync(context, logger);
            await OrganizationSeeder.SeedAsync(context, logger);

            // Seed location data
            await LocationSeeder.SeedAllAsync(context, logger);
            
            logger.LogInformation("Complete database seeding process finished successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during complete database seeding process: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Runs only the role and organization seeder.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public static async Task SeedRolesAndOrganizationsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting role and organization seeding...");
        
        try
        {
            await RoleSeeder.SeedAsync(context, logger);
            await OrganizationSeeder.SeedAsync(context, logger);
            logger.LogInformation("Role and organization seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during role and organization seeding: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Runs only the location seeder.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public static async Task SeedLocationsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting location seeding...");
        
        try
        {
            await LocationSeeder.SeedAllAsync(context, logger);
            logger.LogInformation("Location seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during location seeding: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validates that all required seed data exists in the database.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>True if all required data exists, false otherwise.</returns>
    public static async Task<bool> ValidateSeedDataAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Validating seed data integrity...");
        
        try
        {
            // Check organizations
            var organizationCount = await context.Organizations.CountAsync();
            logger.LogInformation("Found {Count} organizations in database", organizationCount);
            
            // Check roles
            var roleCount = await context.Roles.CountAsync();
            logger.LogInformation("Found {Count} roles in database", roleCount);
            
            // Check specific required entities by their fixed UUIDs
            var nationalOrg = await context.Organizations
                .FirstOrDefaultAsync(o => o.Id == OrganizationSeeder.NationalOrganizationId);
            var regionalOrg = await context.Organizations
                .FirstOrDefaultAsync(o => o.Id == OrganizationSeeder.RegionalOrganizationId);
            
            var adminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Id == RoleSeeder.AdminRoleId);
            var superAdminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Id == RoleSeeder.SuperAdminRoleId);
            var fieldOfficerRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Id == RoleSeeder.FieldOfficerRoleId);
            
            bool isValid = nationalOrg != null && regionalOrg != null && 
                          adminRole != null && superAdminRole != null && fieldOfficerRole != null;
            
            if (isValid)
            {
                logger.LogInformation("All required seed data validated successfully");
            }
            else
            {
                logger.LogWarning("Some required seed data is missing from the database");
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during seed data validation: {Message}", ex.Message);
            return false;
        }
    }
}
