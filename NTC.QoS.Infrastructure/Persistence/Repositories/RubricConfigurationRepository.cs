using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing RubricConfiguration entities
/// </summary>
internal sealed class RubricConfigurationRepository(ApplicationDbContext context)
    : Repository<RubricConfiguration>(context), IRubricConfigurationRepository
{
    public async Task<RubricConfiguration?> GetByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricConfiguration>()
            .FirstOrDefaultAsync(rc => rc.Version == version && !rc.IsDeleted, cancellationToken);
    }

    public async Task<RubricConfiguration?> GetActiveRubricAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricConfiguration>()
            .Include(rc => rc.Criteria.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Thresholds.Where(t => !t.IsDeleted))
            .Include(rc => rc.StatusRanges.Where(sr => !sr.IsDeleted))
            .FirstOrDefaultAsync(rc => rc.IsActive && !rc.IsDeleted, cancellationToken);
    }

    public async Task<RubricConfiguration?> GetDefaultRubricAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricConfiguration>()
            .Include(rc => rc.Criteria.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Thresholds.Where(t => !t.IsDeleted))
            .Include(rc => rc.StatusRanges.Where(sr => !sr.IsDeleted))
            .FirstOrDefaultAsync(rc => rc.IsDefault && !rc.IsDeleted, cancellationToken);
    }

    public async Task<RubricConfiguration?> GetEffectiveRubricForDateAsync(DateTime effectiveDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricConfiguration>()
            .Include(rc => rc.Criteria.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Thresholds.Where(t => !t.IsDeleted))
            .Include(rc => rc.StatusRanges.Where(sr => !sr.IsDeleted))
            .Where(rc => !rc.IsDeleted
                && rc.IsActive
                && rc.EffectiveFrom <= effectiveDate
                && (rc.EffectiveTo == null || rc.EffectiveTo >= effectiveDate))
            .OrderByDescending(rc => rc.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RubricConfiguration?> GetByIdWithRelatedAsync(Guid rubricId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricConfiguration>()
            .Include(rc => rc.Criteria.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Thresholds.Where(t => !t.IsDeleted))
            .Include(rc => rc.StatusRanges.Where(sr => !sr.IsDeleted))
            .FirstOrDefaultAsync(rc => rc.Id == rubricId && !rc.IsDeleted, cancellationToken);
    }
}
