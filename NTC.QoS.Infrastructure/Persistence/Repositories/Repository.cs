using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implements the IRepository interface using Entity Framework Core for data access.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();
            if (spec != null)
            {
                Repository<TEntity>.ApplySpecification(query, spec);
            }
            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<TEntity> GetByIdAsync(Guid id, ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (spec != null)
            {
                Repository<TEntity>.ApplySpecification(query, spec);
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken) ?? throw new InvalidOperationException("Entity not found");
        }


        public async Task<List<TEntity>> ListAsync(ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (spec != null)
            {
                Repository<TEntity>.ApplySpecification(query, spec);
            }

            return await query.ToListAsync(cancellationToken);
        }
       
        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static void ApplySpecification(IQueryable<TEntity> query, ISpecification<TEntity> spec)
        {
            if (spec.IsNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Includes
            foreach (var include in spec.Includes)
            {
                query = query.Include(include);
            }

            // ThenIncludes
            foreach (var thenInclude in spec.ThenIncludes)
            {
                query = query.Include(thenInclude.Item1).ThenInclude(thenInclude.Item2);
            }
        }

    }
}