using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for managing wireless broadband report jobs
/// </summary>
internal sealed class WirelessBroadbandReportJobRepository : Repository<WirelessBroadbandReportJob>, IWirelessBroadbandReportJobRepository
{
    public WirelessBroadbandReportJobRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<WirelessBroadbandReportJob?> GetByHangfireJobIdAsync(string hangfireJobId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WirelessBroadbandReportJob>()
            .FirstOrDefaultAsync(j => j.HangfireJobId == hangfireJobId && !j.IsDeleted, cancellationToken);
    }

    public async Task<List<WirelessBroadbandReportJob>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WirelessBroadbandReportJob>()
            .Where(j => j.UserId == userId && !j.IsDeleted)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WirelessBroadbandReportJob>> GetByStatusAsync(ReportJobStatusEnum status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WirelessBroadbandReportJob>()
            .Where(j => j.Status == status && !j.IsDeleted)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateJobStatusAsync(
        string hangfireJobId, 
        ReportJobStatusEnum status, 
        string? filePath = null,
        string? fileName = null,
        string? errorMessage = null,
        DateTime? completedAt = null,
        int? barangaysProcessed = null,
        long? processingTimeMs = null,
        CancellationToken cancellationToken = default)
    {
        var job = await GetByHangfireJobIdAsync(hangfireJobId, cancellationToken);
        if (job == null) return;

        job.Status = status;
        
        if (filePath != null) job.FilePath = filePath;
        if (fileName != null) job.FileName = fileName;
        if (errorMessage != null) job.ErrorMessage = errorMessage;
        if (completedAt.HasValue) job.CompletedAt = completedAt;
        if (barangaysProcessed.HasValue) job.BarangaysProcessed = barangaysProcessed.Value;
        if (processingTimeMs.HasValue) job.ProcessingTimeMs = processingTimeMs.Value;

        if (status == ReportJobStatusEnum.Processing && job.StartedAt == null)
        {
            job.StartedAt = DateTime.UtcNow;
        }

        Update(job);
        await SaveChangesAsync(cancellationToken);
    }
}
