using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for triggering connectivity assessments after entity changes.
/// Encapsulates logic for determining region and enqueuing assessment jobs.
/// </summary>
public class ConnectivityAssessmentTriggerService : IConnectivityAssessmentTriggerService
{
    private readonly IBarangayRepository _barangayRepository;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<ConnectivityAssessmentTriggerService> _logger;

    public ConnectivityAssessmentTriggerService(
        IBarangayRepository barangayRepository,
        IBackgroundJobService backgroundJobService,
        ILogger<ConnectivityAssessmentTriggerService> logger)
    {
        _barangayRepository = barangayRepository;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Triggers a connectivity assessment for the region containing the specified location.
    /// If regionId is provided, uses it directly; otherwise performs spatial lookup.
    /// </summary>
    public async Task TriggerAssessmentAsync(
        Point location,
        Guid entityId,
        string entityType,
        Guid? providedRegionId,
        CancellationToken cancellationToken)
    {
        Guid? regionId = providedRegionId;

        // If region not provided, fallback to spatial lookup
        if (!regionId.HasValue)
        {
            _logger.LogDebug(
                "No region ID provided in request, performing spatial lookup for {EntityType} {EntityId}",
                entityType, entityId);

            var barangay = await _barangayRepository.FindBarangayByLocationWithHierarchyAsync(location, cancellationToken);
            regionId = barangay?.Municipality?.Province?.RegionId;
        }

        if (regionId.HasValue)
        {
            _logger.LogInformation(
                "Triggering connectivity assessment for region {RegionId} after {EntityType} operation",
                regionId.Value, entityType);

            _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                job => job.ExecuteForRegionAsync(regionId.Value, cancellationToken));
        }
        else
        {
            _logger.LogWarning(
                "Could not determine region for {EntityType} {EntityId} at location {Location}. Assessment not triggered.",
                entityType, entityId, location?.Coordinate);
        }
    }
}
