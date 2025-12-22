using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for connectivity gap report processing background job
/// </summary>
public interface IProcessConnectivityGapReportJob
{
    /// <summary>
    /// Executes the connectivity gap report generation job
    /// </summary>
    /// <param name="jobRequest">Job request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(ConnectivityGapReportJobRequestDto jobRequest, CancellationToken cancellationToken);
}
