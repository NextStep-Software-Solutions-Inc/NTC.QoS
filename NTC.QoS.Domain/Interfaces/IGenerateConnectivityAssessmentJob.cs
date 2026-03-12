using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Interfaces;

public interface IGenerateConnectivityAssessmentJob : IBackgroundJob<Guid>
{
    /// <summary>
    /// Triggers connectivity assessment for a specific region (for manual operations like cell creation/deletion).
    /// </summary>
    /// <param name="regionId">The region ID to generate assessment for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteForRegionAsync(Guid regionId, CancellationToken cancellationToken);
}