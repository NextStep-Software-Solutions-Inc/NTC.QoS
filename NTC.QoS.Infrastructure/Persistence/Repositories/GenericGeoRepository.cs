using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

public class GenericGeoRepository<T> : IGenericGeoRepository<T> where T : class, IGeoBoundaryEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericGeoRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsNoTracking();
    }
}
