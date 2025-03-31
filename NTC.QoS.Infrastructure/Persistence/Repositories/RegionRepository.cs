using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class RegionRepository(ApplicationDbContext context) : Repository<Region>(context), IRegionRepository
{
    public override async Task<Region?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Region>().Where(e => !EF.Property<bool>(e, "IsDeleted"));
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public override async Task<List<Region>> ListAsync(
        Expression<Func<Region, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Region> query = _context.Set<Region>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"));

        if (expression is not null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<Region>> GetAllRegionsAsync(CancellationToken cancellationToken = default)
    {
        return await ListAsync(null, cancellationToken);
    }
}