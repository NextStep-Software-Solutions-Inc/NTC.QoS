using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for managing RubricThreshold entities
/// </summary>
public interface IRubricThresholdRepository : IRepository<RubricThreshold>
{
    /// <summary>
    /// Gets all thresholds for a specific criterion
    /// </summary>
    /// <param name="criterionId">The criterion ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of thresholds ordered by SortOrder</returns>
    Task<List<RubricThreshold>> GetByCriterionIdAsync(Guid criterionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all thresholds for multiple criteria
    /// </summary>
    /// <param name="criterionIds">The list of criterion IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of thresholds ordered by CriterionId, then SortOrder</returns>
    Task<List<RubricThreshold>> GetByCriterionIdsAsync(List<Guid> criterionIds, CancellationToken cancellationToken = default);
}
