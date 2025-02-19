using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        TEntity? GetById(int id);
        Task<List<TEntity>> GetAll(CancellationToken cancellationToken);
        Task<TEntity> AddAsync(TEntity entity);
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
        Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entities);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
