using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;
using System.Threading;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class GenerationBandRepository(ApplicationDbContext context) : Repository<GenerationBand>(context), IGenerationBandRepository
{
    public new async Task<GenerationBand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<GenerationBand>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(gb => gb.Generation)
            .Include(gb => gb.Band);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public new async Task<List<GenerationBand>> ListAsync(
        Expression<Func<GenerationBand, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<GenerationBand> query = _context.Set<GenerationBand>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(gb => gb.Generation)
            .Include(gb => gb.Band);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<GenerationBand?> GetGenerationBandIdAsync(string generation, string band, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GenerationBand>()
            .Where(gb => !EF.Property<bool>(gb, "IsDeleted") &&
                         gb.Generation != null && gb.Generation.Name.Contains(generation) &&
                         gb.Band != null && (gb.Band.Name != null && gb.Band.Name.Contains(band) ||
                                             gb.Band.Description != null && gb.Band.Description.Contains(band)))
            .Include(gb => gb.Generation)
            .Include(gb => gb.Band)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public override IQueryable<GenerationBand> Query(Expression<Func<GenerationBand, bool>>? expression = null)
    {
        var query = _context.Set<GenerationBand>()
            .Include(gb => gb.Band)
            .Include(gb => gb.Generation)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
