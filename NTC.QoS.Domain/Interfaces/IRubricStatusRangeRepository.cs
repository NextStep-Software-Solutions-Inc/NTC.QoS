using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for managing RubricStatusRange entities
/// </summary>
public interface IRubricStatusRangeRepository : IRepository<RubricStatusRange>
{
    /// <summary>
    /// Gets all status ranges for a specific rubric configuration
    /// </summary>
    /// <param name="rubricConfigurationId">The rubric configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of status ranges ordered by SortOrder</returns>
    Task<List<RubricStatusRange>> GetByRubricIdAsync(Guid rubricConfigurationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status range that matches a given score
    /// </summary>
    /// <param name="rubricConfigurationId">The rubric configuration ID</param>
    /// <param name="score">The total score to evaluate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching status range if found, null otherwise</returns>
    Task<RubricStatusRange?> GetStatusForScoreAsync(Guid rubricConfigurationId, int score, CancellationToken cancellationToken = default);
}
