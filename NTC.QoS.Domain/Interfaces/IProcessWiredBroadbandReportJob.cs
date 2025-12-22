using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for background wired broadband report generation job processing
/// </summary>
public interface IProcessWiredBroadbandReportJob
{
    /// <summary>
    /// Execute wired broadband report generation as a background job
    /// </summary>
    /// <param name="jobRequest">Job request containing report parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteAsync(WiredBroadbandReportJobRequestDto jobRequest, CancellationToken cancellationToken);
}
