using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class SectorRepository(ApplicationDbContext context) 
    : Repository<Sector>(context), ISectorRepository
{
    public override async Task<Sector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Sector>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(s => s.Network)
            .Include(s => s.MIMO)
            .Include(s => s.Cell).ThenInclude(c => c!.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(s => s.Cell).ThenInclude(c => c!.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(s => s.TxAntennaPattern)
            .Include(s => s.RxAntennaPattern);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<Sector>> ListAsync(
        Expression<Func<Sector, bool>>? expression = null, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<Sector> query = _context.Set<Sector>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(s => s.Network)
            .Include(s => s.MIMO)
            .Include(s => s.Cell).ThenInclude(c => c!.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(s => s.Cell).ThenInclude(c => c!.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(s => s.TxAntennaPattern)
            .Include(s => s.RxAntennaPattern);

        if (expression is not null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }

    // Example of how to get all sectors
    public async Task<List<Sector>> GetAllSectorsAsync(CancellationToken cancellationToken = default)
    {
        return await ListAsync(null, cancellationToken);
    }

    // Example of how to find sectors based on a condition
    public async Task<List<Sector>> FindSectorsAsync(Expression<Func<Sector, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await ListAsync(expression, cancellationToken);
    }
}
