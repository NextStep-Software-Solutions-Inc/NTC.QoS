using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for managing connectivity assessment schedulers
/// </summary>
internal sealed class ConnectivityAssessmentSchedulerRepository : Repository<ConnectivityAssessmentScheduler>, IConnectivityAssessmentSchedulerRepository
{
    public ConnectivityAssessmentSchedulerRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get all active schedulers
    /// </summary>
    public async Task<IEnumerable<ConnectivityAssessmentScheduler>> GetActiveSchedulersAsync()
    {
        return await _context.Set<ConnectivityAssessmentScheduler>()
            .Where(s => s.Status == SchedulerStatusEnum.Active && !s.IsDeleted)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get scheduler by Hangfire job ID
    /// </summary>
    public async Task<ConnectivityAssessmentScheduler?> GetByHangfireJobIdAsync(string hangfireJobId)
    {
        return await _context.Set<ConnectivityAssessmentScheduler>()
            .FirstOrDefaultAsync(s => s.HangfireJobId == hangfireJobId && !s.IsDeleted);
    }

    /// <summary>
    /// Get schedulers by status
    /// </summary>
    public async Task<IEnumerable<ConnectivityAssessmentScheduler>> GetByStatusAsync(SchedulerStatusEnum status)
    {
        return await _context.Set<ConnectivityAssessmentScheduler>()
            .Where(s => s.Status == status && !s.IsDeleted)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Update scheduler execution statistics
    /// </summary>
    public async Task UpdateExecutionStatsAsync(Guid schedulerId, bool success, string? errorMessage = null)
    {
        var scheduler = await _context.Set<ConnectivityAssessmentScheduler>()
            .FirstOrDefaultAsync(s => s.Id == schedulerId && !s.IsDeleted);

        if (scheduler == null)
        {
            return;
        }

        scheduler.LastExecutionTime = DateTime.UtcNow;
        scheduler.ExecutionCount++;

        if (success)
        {
            scheduler.LastError = null;
        }
        else
        {
            scheduler.FailureCount++;
            scheduler.LastError = errorMessage;
            
            // Pause scheduler if it fails too many times
            if (scheduler.FailureCount >= 5)
            {
                scheduler.Status = SchedulerStatusEnum.Failed;
            }
        }

        _context.Set<ConnectivityAssessmentScheduler>().Update(scheduler);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get schedulers that need to be processed (between start and end dates, active status)
    /// </summary>
    public async Task<IEnumerable<ConnectivityAssessmentScheduler>> GetSchedulersToProcessAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Set<ConnectivityAssessmentScheduler>()
            .Where(s => 
                s.Status == SchedulerStatusEnum.Active &&
                !s.IsDeleted &&
                (s.StartDate == null || s.StartDate <= now) &&
                (s.EndDate == null || s.EndDate >= now))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
