using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing RubricCriterion entities
/// </summary>
internal sealed class RubricCriterionRepository(ApplicationDbContext context)
    : Repository<RubricCriterion>(context), IRubricCriterionRepository
{
    public async Task<List<RubricCriterion>> GetByRubricIdAsync(Guid rubricConfigurationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricCriterion>()
            .Where(rc => rc.RubricConfigurationId == rubricConfigurationId && !rc.IsDeleted)
            .OrderBy(rc => rc.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RubricCriterion>> GetEnabledByRubricIdAsync(Guid rubricConfigurationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricCriterion>()
            .Where(rc => rc.RubricConfigurationId == rubricConfigurationId && rc.IsEnabled && !rc.IsDeleted)
            .OrderBy(rc => rc.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<RubricCriterion?> GetByCriterionTypeAsync(Guid rubricConfigurationId, CriterionTypeEnum criterionType, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricCriterion>()
            .FirstOrDefaultAsync(rc => rc.RubricConfigurationId == rubricConfigurationId 
                && rc.CriterionType == criterionType 
                && !rc.IsDeleted, cancellationToken);
    }
}
