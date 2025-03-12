using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class LinkRepository(ApplicationDbContext context) : Repository<Link>(context), ILinkRepository
{
    public async Task<bool> ValidateLinkTypesAsync(Guid? uplinkId, Guid? downlinkId, CancellationToken cancellationToken = default)
    {
        if (uplinkId.HasValue)
        {
            var uplink = await _context.Set<Link>()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => EF.Property<Guid>(l, "Id") == uplinkId.Value && l.Type == LinkTypeEnum.Uplink, cancellationToken);

            if (uplink is null)
            {
                return false;
            }
            if (uplink.IsDeleted)
            {
                return false;
            }
        }

        if (downlinkId.HasValue)
        {
            var downlink = await _context.Set<Link>()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => EF.Property<Guid>(l, "Id") == downlinkId.Value && l.Type == LinkTypeEnum.Downlink, cancellationToken);

            if (downlink is null)
            {
                return false;
            }
            if (downlink.IsDeleted)
            {
                return false;
            }
        }

        return true;
    }
    public override async Task<Link?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Link>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(l => l.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(l => l.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(l => l.Band);
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<Link>> ListAsync(
        Expression<Func<Link, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Link> query = _context.Set<Link>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(l => l.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(l => l.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(l => l.Band);

        if (expression is not null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}