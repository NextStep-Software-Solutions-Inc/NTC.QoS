using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NetworkRepository(ApplicationDbContext context) 
    : Repository<Network>(context), INetworkRepository
{
    public override IQueryable<Network> Query(Expression<Func<Network, bool>>? expression = null)
    {
        var query = _context.Set<Network>()
            .Include(n => n.NetworkTemplate)
            .Include(n => n.SystemType)
            .Include(n => n.UpLink)
            .Include(n => n.DownLink)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;

    }
}

