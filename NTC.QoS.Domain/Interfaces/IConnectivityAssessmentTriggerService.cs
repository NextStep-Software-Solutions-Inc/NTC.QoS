using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for triggering connectivity assessments after entity changes.
/// Encapsulates logic for determining region and enqueuing assessment jobs.
/// </summary>
public interface IConnectivityAssessmentTriggerService
{
    /// <summary>
    /// Triggers a connectivity assessment for the region containing the specified location.
    /// If regionId is provided, uses it directly; otherwise performs spatial lookup.
    /// </summary>
    /// <param name="location">The geographic location of the entity.</param>
    /// <param name="entityId">The ID of the entity (NAP, Cell, Speedtest, etc.).</param>
    /// <param name="entityType">The type of entity (e.g., "NAP", "Cell", "Speedtest").</param>
    /// <param name="providedRegionId">Optional region ID provided by the caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TriggerAssessmentAsync(
        Point location,
        Guid entityId,
        string entityType,
        Guid? providedRegionId,
        CancellationToken cancellationToken);
}
