using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for managing connectivity assessment schedulers
/// </summary>
public interface IConnectivityAssessmentSchedulerRepository : IRepository<ConnectivityAssessmentScheduler>
{
    /// <summary>
    /// Get all active schedulers
    /// </summary>
    Task<IEnumerable<ConnectivityAssessmentScheduler>> GetActiveSchedulersAsync();

    /// <summary>
    /// Get scheduler by Hangfire job ID
    /// </summary>
    Task<ConnectivityAssessmentScheduler?> GetByHangfireJobIdAsync(string hangfireJobId);

    /// <summary>
    /// Get schedulers by status
    /// </summary>
    Task<IEnumerable<ConnectivityAssessmentScheduler>> GetByStatusAsync(SchedulerStatusEnum status);

    /// <summary>
    /// Update scheduler execution statistics
    /// </summary>
    Task UpdateExecutionStatsAsync(Guid schedulerId, bool success, string? errorMessage = null);

    /// <summary>
    /// Get schedulers that need to be processed (between start and end dates, active status)
    /// </summary>
    Task<IEnumerable<ConnectivityAssessmentScheduler>> GetSchedulersToProcessAsync();
}
