using NTC.QoS.Domain.Models.DTOs;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<PaginatedResponse<TEntity>> GetAll(int pageNumber, int pageSize, CancellationToken cancellationToken);

        Task<TEntity> AddAsync(TEntity entity);
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
        Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entities);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
