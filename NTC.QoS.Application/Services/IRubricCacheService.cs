using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Service interface for caching and retrieving rubric configurations
/// </summary>
public interface IRubricCacheService
{
    /// <summary>
    /// Gets the currently active rubric configuration with all related entities from cache or database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The active rubric configuration with criteria, thresholds, and status ranges</returns>
    Task<RubricConfiguration?> GetActiveRubricAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a rubric configuration by version string from cache or database
    /// </summary>
    /// <param name="version">The version string (e.g., "1.0.0")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rubric configuration with all related entities</returns>
    Task<RubricConfiguration?> GetRubricByVersionAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a rubric configuration by ID from cache or database
    /// </summary>
    /// <param name="rubricId">The rubric configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rubric configuration with all related entities</returns>
    Task<RubricConfiguration?> GetRubricByIdAsync(Guid rubricId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all rubric cache entries
    /// </summary>
    Task InvalidateCacheAsync();

    /// <summary>
    /// Refreshes the active rubric in the cache by reloading from database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refreshed active rubric configuration</returns>
    Task<RubricConfiguration?> RefreshActiveRubricAsync(CancellationToken cancellationToken = default);
}
