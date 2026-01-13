using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for managing RubricCriterion entities
/// </summary>
public interface IRubricCriterionRepository : IRepository<RubricCriterion>
{
    /// <summary>
    /// Gets all criteria for a specific rubric configuration
    /// </summary>
    /// <param name="rubricConfigurationId">The rubric configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of criteria ordered by SortOrder</returns>
    Task<List<RubricCriterion>> GetByRubricIdAsync(Guid rubricConfigurationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled criteria for a specific rubric configuration
    /// </summary>
    /// <param name="rubricConfigurationId">The rubric configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of enabled criteria ordered by SortOrder</returns>
    Task<List<RubricCriterion>> GetEnabledByRubricIdAsync(Guid rubricConfigurationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific criterion by rubric ID and criterion type
    /// </summary>
    /// <param name="rubricConfigurationId">The rubric configuration ID</param>
    /// <param name="criterionType">The criterion type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The criterion if found, null otherwise</returns>
    Task<RubricCriterion?> GetByCriterionTypeAsync(Guid rubricConfigurationId, CriterionTypeEnum criterionType, CancellationToken cancellationToken = default);
}
