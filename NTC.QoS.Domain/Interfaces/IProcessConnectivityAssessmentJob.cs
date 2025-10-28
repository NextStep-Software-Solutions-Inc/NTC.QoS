using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for background connectivity assessment job processing
/// </summary>
public interface IProcessConnectivityAssessmentJob
{
    /// <summary>
    /// Execute connectivity assessment generation as a background job
    /// </summary>
    /// <param name="jobRequest">Job request containing assessment parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteAsync(ConnectivityAssessmentJobRequestDto jobRequest, CancellationToken cancellationToken);
}
