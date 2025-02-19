using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly ApplicationDbContext _context;

        protected readonly DbSet<TEntity> DbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            DbSet = ((DbSet<TEntity>?)context.Set<TEntity>()) ?? throw new InvalidOperationException();
        }

        public TEntity? GetById(int id)
        {
            return DbSet.Find(id);
        }
        public async Task<List<TEntity>> GetAll(CancellationToken cancellationToken)
        {
            return await DbSet.ToListAsync(cancellationToken);
        }


        public async Task<TEntity> AddAsync(TEntity entity)
        {
            DbSet.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await DbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            DbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return _context.Set<TEntity>().Where(predicate);
        }
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _context.Set<TEntity>().AnyAsync(predicate);
        }
        public async Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entities)
        {
            _context.UpdateRange(entities);
            await _context.SaveChangesAsync();
            return entities;
        }
        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

}