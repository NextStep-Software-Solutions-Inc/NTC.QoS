using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class WirelessValidationRepository(ApplicationDbContext context) : Repository<WirelessValidation>(context), IWirelessValidationRepository
{
    public override IQueryable<WirelessValidation> Query(Expression<Func<WirelessValidation, bool>>? expression = null)
    {
        var query = _context.Set<WirelessValidation>()
            .Include(wv => wv.Technology)
            .Include(st => st.ServiceProvider)
                .ThenInclude(sp => sp.Service)
            .Include(st => st.ServiceProvider)
                .ThenInclude(sp => sp.Provider)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}
