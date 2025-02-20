using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories
{
    internal sealed class ProvinceRepository(ApplicationDbContext context) : Repository<Province>(context), IProvinceRepository
    {
        public override async Task<Province?> FindAsync(Expression<Func<Province, bool>> expression, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Province>().Where(expression)
                .Include(s => s.Region);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

    }
}
