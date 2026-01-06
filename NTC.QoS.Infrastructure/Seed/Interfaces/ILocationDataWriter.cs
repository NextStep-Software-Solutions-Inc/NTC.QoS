namespace NTC.QoS.Infrastructure.Seed.Interfaces;

/// <summary>
/// Persists entities to the database in batches.
/// </summary>
/// <typeparam name="TEntity">The entity type to persist.</typeparam>
public interface ILocationDataWriter<TEntity> where TEntity : class
{
    /// <summary>
    /// Writes entities in batches asynchronously.
    /// </summary>
    /// <param name="entities">Entities to write.</param>
    /// <param name="options">Write options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entities written.</returns>
    Task<int> WriteBatchAsync(
        IAsyncEnumerable<TEntity> entities,
        WriteOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for batch writing operations.
/// </summary>
public record WriteOptions
{
    public int BatchSize { get; init; } = 100;
    public int TimeoutSeconds { get; init; } = 300;
    public bool EnableStreaming { get; init; } = true;
    public bool ForceReseed { get; init; } = false;
    public Action<int>? ProgressCallback { get; init; }
}
