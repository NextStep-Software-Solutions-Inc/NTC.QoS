using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class SpeedtestRepository(ApplicationDbContext context)
    : Repository<Speedtest>(context), ISpeedtestRepository
{
    public override async Task<Speedtest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Speedtest>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(s => s.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(s => s.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(s => s.Barangay).ThenInclude(b => b!.Municipality).ThenInclude(m => m!.Province).ThenInclude(p => p!.Region)
            .Include(s => s.Generation).ThenInclude(g =>g!.Technologies)
            .Include(s => s.File);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<Speedtest>> ListAsync(
        Expression<Func<Speedtest, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Speedtest> query = _context.Set<Speedtest>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(s => s.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(s => s.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(s => s.Barangay).ThenInclude(b => b!.Municipality).ThenInclude(m => m!.Province).ThenInclude(p => p!.Region)
            .Include(s => s.Generation).ThenInclude(g => g!.Technologies)
            .Include(s => s.File);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }
}

