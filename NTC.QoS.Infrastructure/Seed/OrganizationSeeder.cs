using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;
using EFCore.BulkExtensions;

namespace NTC.QoS.Infrastructure.Seed;

public static class OrganizationSeeder
{
    // Fixed UUIDs for consistent organization seeding
    public static readonly Guid NationalOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid RegionalOrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// Seeds organizations into the database with fixed UUIDs.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="logger">The logger instance.</param>
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting Organization seeding process...");

        await SeedOrganizationsAsync(context, logger);

        logger.LogInformation("Organization seeding process completed.");
    }

    /// <summary>
    /// Seeds organizations into the database.
    /// </summary>
    private static async Task SeedOrganizationsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Checking if organizations need to be seeded...");

        // Check if organizations already exist
        var existingCount = await context.Organizations.AsNoTracking().CountAsync();
        if (existingCount > 0)
        {
            logger.LogInformation("Organizations already exist in database. Skipping organization seeding.");
            return;
        }

        logger.LogInformation("Seeding organizations...");

        var organizations = new List<Organization>
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
                Name = "Regional or National",
                Description = "Regional or National level organization",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            }
        };

        await BulkInsertWithRetry(context, organizations, "Organizations", logger);
    }

    /// <summary>
    /// Bulk inserts entities with retry logic for better reliability.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="dbContext">The database context.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="entityType">The entity type name for logging.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="batchSize">Batch size for bulk operations.</param>
    private static async Task BulkInsertWithRetry<T>(ApplicationDbContext dbContext, List<T> entities, 
        string entityType, ILogger logger, int maxRetries = 3, int batchSize = 1000) where T : class
    {
        if (!entities.Any())
        {
            logger.LogWarning("No {EntityType} to insert.", entityType);
            return;
        }

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                logger.LogInformation("Inserting {Count} {EntityType} (attempt {Attempt}/{MaxAttempts})...", 
                    entities.Count, entityType, retry + 1, maxRetries);

                // Process in batches for better performance with large datasets
                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    await dbContext.BulkInsertAsync(batch);
                    
                    if (batch.Count == batchSize) // Only log for full batches to reduce noise
                    {
                        logger.LogInformation("Inserted batch {BatchStart}-{BatchEnd} of {Total} {EntityType}", 
                            i + 1, i + batch.Count, entities.Count, entityType);
                    }
                }

                await dbContext.SaveChangesAsync();
                logger.LogInformation("Successfully seeded {Count} {EntityType} into the database.", entities.Count, entityType);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving {EntityType} to database (attempt {Attempt}/{MaxAttempts}): {Message}", 
                    entityType, retry + 1, maxRetries, ex.Message);
                
                if (retry == maxRetries - 1)
                {
                    throw;
                }
                
                await Task.Delay(1000); // Wait 1 second before retry
            }
        }
        
        throw new InvalidOperationException($"Failed to save {entityType} after {maxRetries} attempts.");
    }
}
