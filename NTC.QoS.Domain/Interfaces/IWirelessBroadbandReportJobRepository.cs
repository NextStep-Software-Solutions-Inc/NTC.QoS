using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for wireless broadband report jobs
/// </summary>
public interface IWirelessBroadbandReportJobRepository : IRepository<WirelessBroadbandReportJob>
{
    /// <summary>
    /// Get job by Hangfire job ID
    /// </summary>
    Task<WirelessBroadbandReportJob?> GetByHangfireJobIdAsync(string hangfireJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get jobs by user ID
    /// </summary>
    Task<List<WirelessBroadbandReportJob>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get jobs by status
    /// </summary>
    Task<List<WirelessBroadbandReportJob>> GetByStatusAsync(ReportJobStatusEnum status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update job status
    /// </summary>
    Task UpdateJobStatusAsync(
        string hangfireJobId, 
        ReportJobStatusEnum status, 
        string? filePath = null,
        string? fileName = null,
        string? errorMessage = null,
        DateTime? completedAt = null,
        int? barangaysProcessed = null,
        long? processingTimeMs = null,
        CancellationToken cancellationToken = default);
}
