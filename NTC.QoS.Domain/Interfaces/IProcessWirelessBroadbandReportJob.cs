using NTC.QoS.Domain.Enums;
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
    /// <param name="jobId">Unique identifier for this job execution</param>
    /// <param name="userId">User who requested the report</param>
    /// <param name="regionId">Optional region ID</param>
    /// <param name="provinceId">Optional province ID</param>
    /// <param name="municipalityId">Optional municipality ID</param>
    /// <param name="format">Report format (PDF or Excel)</param>
    /// <param name="filterByStatus">Optional filter by service status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteAsync(
        string jobId,
        string userId,
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        ReportFormatEnum format,
        ServiceStatusEnum? filterByStatus,
        CancellationToken cancellationToken);
}
