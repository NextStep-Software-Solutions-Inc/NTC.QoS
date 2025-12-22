using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for connectivity report processing background job
/// </summary>
public interface IProcessConnectivityReportJob
{
    /// <summary>
    /// Executes the connectivity report generation job
    /// </summary>
    /// <param name="jobRequest">Job request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(ConnectivityReportJobRequestDto jobRequest, CancellationToken cancellationToken);
}
