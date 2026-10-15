using Microsoft.Extensions.Logging;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

/// <summary>
/// Seeds authorization data. Permissions are defined in-memory via PermissionsRegistry.
/// System roles and their default permissions are seeded via RoleSeeder.
/// </summary>
public static class AuthorizationSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting authorization data seeding via static PermissionsRegistry...");
            await RoleSeeder.SeedAsync(context, logger);
            logger.LogInformation("Authorization data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding authorization data");
            throw;
        }
    }
}