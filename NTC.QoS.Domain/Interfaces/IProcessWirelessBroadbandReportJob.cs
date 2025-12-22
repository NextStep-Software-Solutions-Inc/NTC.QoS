using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Background job interface for processing wireless broadband scored reports asynchronously
/// </summary>
public interface IProcessWirelessBroadbandReportJob
{
    /// <summary>
    /// Executes the wireless broadband report generation job
    /// </summary>
    /// <param name="jobRequest">Job request containing report parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteAsync(WirelessBroadbandReportJobRequestDto jobRequest, CancellationToken cancellationToken);
}
