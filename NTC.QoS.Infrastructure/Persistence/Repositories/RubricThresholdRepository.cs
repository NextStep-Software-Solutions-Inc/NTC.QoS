using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing RubricThreshold entities
/// </summary>
internal sealed class RubricThresholdRepository(ApplicationDbContext context)
    : Repository<RubricThreshold>(context), IRubricThresholdRepository
{
    public async Task<List<RubricThreshold>> GetByCriterionIdAsync(Guid criterionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricThreshold>()
            .Where(rt => rt.RubricCriterionId == criterionId && !rt.IsDeleted)
            .OrderBy(rt => rt.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RubricThreshold>> GetByCriterionIdsAsync(List<Guid> criterionIds, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricThreshold>()
            .Where(rt => criterionIds.Contains(rt.RubricCriterionId) && !rt.IsDeleted)
            .OrderBy(rt => rt.RubricCriterionId)
            .ThenBy(rt => rt.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
