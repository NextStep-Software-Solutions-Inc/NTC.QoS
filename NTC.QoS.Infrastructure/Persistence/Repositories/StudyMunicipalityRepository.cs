using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class StudyMunicipalityRepository(ApplicationDbContext context)
    : Repository<StudyMunicipality>(context), IStudyMunicipalityRepository
{
    public override async Task<StudyMunicipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<StudyMunicipality>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sm => sm.Municipality).ThenInclude(m => m!.Province).ThenInclude(p => p!.Region)
            .Include(sm => sm.Study);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<StudyMunicipality>> ListAsync(
        Expression<Func<StudyMunicipality, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StudyMunicipality> query = _context.Set<StudyMunicipality>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sm => sm.Municipality).ThenInclude(m => m!.Province).ThenInclude(p => p!.Region)
            .Include(sm => sm.Study);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }
    public override async Task<StudyMunicipality?> FindAsync(Expression<Func<StudyMunicipality, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<StudyMunicipality>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sm => sm.Municipality).ThenInclude(m => m!.Province).ThenInclude(p => p!.Region)
            .Include(sm => sm.Study);

        return await query.FirstOrDefaultAsync(expression, cancellationToken);
    }
}

