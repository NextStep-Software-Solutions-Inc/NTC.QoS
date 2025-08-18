using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TResult?> FirstOrDefaultAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);

    [Obsolete("FindAsync is deprecated. Use the Query method with appropriate filters instead then call FirstOrDefaultAsync().")]
    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

    [Obsolete("GetByIdAsync is deprecated. Use the Query method with appropriate filters instead then call FirstOrDefaultAsync().")]
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [Obsolete("ListAsync is deprecated. Use the Query method with appropriate filters instead then call ToListAsync or PaginateAsync.")]
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

    IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? expression = null);
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<TEntity> entities);
    Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResponse<TResult>> PaginateAsync<TResult>(IQueryable<TResult> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> ExecuteDeleteAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);
    IQueryable<TEntity> QueryWithIgnoreQueryFilters(Expression<Func<TEntity, bool>>? expression = null);
    IQueryable<TEntity> QueryAsNoTracking(Expression<Func<TEntity, bool>>? expression = null);
    Task BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
}
