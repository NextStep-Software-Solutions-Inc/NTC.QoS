using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed.Interfaces;

namespace NTC.QoS.Infrastructure.Seed.Writers;

/// <summary>
/// Writes location entities in optimized batches with configurable options.
/// </summary>
/// <typeparam name="TEntity">The entity type to write.</typeparam>
public class BatchedLocationWriter<TEntity> : ILocationDataWriter<TEntity> where TEntity : class
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BatchedLocationWriter<TEntity>> _logger;

    public BatchedLocationWriter(
        ApplicationDbContext dbContext,
        ILogger<BatchedLocationWriter<TEntity>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> WriteBatchAsync(
        IAsyncEnumerable<TEntity> entities,
        WriteOptions options,
        CancellationToken cancellationToken = default)
    {
        var totalWritten = 0;
        var batch = new List<TEntity>(options.BatchSize);

        await foreach (var entity in entities.WithCancellation(cancellationToken))
        {
            batch.Add(entity);

            if (batch.Count >= options.BatchSize)
            {
                var written = await WriteSingleBatchAsync(batch, options, cancellationToken);
                totalWritten += written;

                options.ProgressCallback?.Invoke(totalWritten);

                batch.Clear();
            }
        }

        // Write remaining entities
        if (batch.Count > 0)
        {
            var written = await WriteSingleBatchAsync(batch, options, cancellationToken);
            totalWritten += written;
            options.ProgressCallback?.Invoke(totalWritten);
        }

        _logger.LogInformation("Completed writing {Count} {EntityType} entities",
            totalWritten, typeof(TEntity).Name);

        return totalWritten;
    }

    private async Task<int> WriteSingleBatchAsync(
        List<TEntity> batch,
        WriteOptions options,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return 0;

        try
        {
            // Set command timeout specifically for seeding operations to handle large geometry data
            var originalTimeout = _dbContext.Database.GetCommandTimeout();
            _dbContext.Database.SetCommandTimeout(options.TimeoutSeconds);

            try
            {
                _logger.LogDebug("Starting to save {Count} {EntityType} entities", batch.Count, typeof(TEntity).Name);
                // Use regular EF insert instead of bulk for large geometries to avoid WKT conversion OOM
                _dbContext.AddRange(batch);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Successfully saved {Count} {EntityType} entities", batch.Count, typeof(TEntity).Name);

                return batch.Count;
            }
            finally
            {
                // Restore original timeout
                _dbContext.Database.SetCommandTimeout(originalTimeout);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write batch of {Count} {EntityType} entities",
                batch.Count, typeof(TEntity).Name);
            throw;
        }
    }
}
