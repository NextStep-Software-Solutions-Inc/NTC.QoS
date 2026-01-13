using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing RubricStatusRange entities
/// </summary>
internal sealed class RubricStatusRangeRepository(ApplicationDbContext context)
    : Repository<RubricStatusRange>(context), IRubricStatusRangeRepository
{
    public async Task<List<RubricStatusRange>> GetByRubricIdAsync(Guid rubricConfigurationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricStatusRange>()
            .Where(rsr => rsr.RubricConfigurationId == rubricConfigurationId && !rsr.IsDeleted)
            .OrderBy(rsr => rsr.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<RubricStatusRange?> GetStatusForScoreAsync(Guid rubricConfigurationId, int score, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RubricStatusRange>()
            .FirstOrDefaultAsync(rsr => rsr.RubricConfigurationId == rubricConfigurationId 
                && rsr.MinScore <= score 
                && rsr.MaxScore >= score 
                && !rsr.IsDeleted, cancellationToken);
    }
}
