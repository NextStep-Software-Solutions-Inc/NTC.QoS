using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for managing RubricConfiguration entities
/// </summary>
public interface IRubricConfigurationRepository : IRepository<RubricConfiguration>
{
    /// <summary>
    /// Gets a rubric configuration by version string
    /// </summary>
    /// <param name="version">The version string (e.g., "1.0.0")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rubric configuration if found, null otherwise</returns>
    Task<RubricConfiguration?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active rubric configuration with all related entities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The active rubric configuration with criteria, thresholds, and status ranges</returns>
    Task<RubricConfiguration?> GetActiveRubricAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default rubric configuration with all related entities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default rubric configuration with criteria, thresholds, and status ranges</returns>
    Task<RubricConfiguration?> GetDefaultRubricAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective rubric configuration for a specific date
    /// </summary>
    /// <param name="effectiveDate">The date to check for effective rubric</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The effective rubric configuration with criteria, thresholds, and status ranges</returns>
    Task<RubricConfiguration?> GetEffectiveRubricForDateAsync(DateTime effectiveDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a rubric configuration by ID with all related entities eagerly loaded
    /// </summary>
    /// <param name="rubricId">The rubric configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rubric configuration with criteria, thresholds, and status ranges</returns>
    Task<RubricConfiguration?> GetByIdWithRelatedAsync(Guid rubricId, CancellationToken cancellationToken = default);
}
