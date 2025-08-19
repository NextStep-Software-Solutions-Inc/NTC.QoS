using EFCore.BulkExtensions;
using Microsoft.Extensions.Logging;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Utility class providing common seeding operations and shared functionality
/// </summary>
public static class SeederUtility
{
    /// <summary>
    /// Performs bulk insert operation with retry logic and batch processing.
    /// </summary>
    /// <typeparam name="T">The entity type to insert</typeparam>
    /// <param name="dbContext">The database context</param>
    /// <param name="entities">The entities to insert</param>
    /// <param name="entityType">The entity type name for logging</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="batchSize">Batch size for bulk operations</param>
    public static async Task BulkInsertWithRetry<T>(ApplicationDbContext dbContext, List<T> entities,
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

                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    await dbContext.BulkInsertAsync(batch);

                    if (batch.Count == batchSize)
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

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retry))); // Exponential backoff
            }
        }
    }
}
