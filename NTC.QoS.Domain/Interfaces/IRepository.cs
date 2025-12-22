using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.Common;
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
    Task<int> CountAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<TEntity> entities);
    Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResponse<TResult>> PaginateAsync<TResult>(IQueryable<TResult> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    [Obsolete("Use ExecuteHardDeleteAsync for physical deletes or SoftDeleteAsync for logical deletes.")]
    Task<int> ExecuteDeleteAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);

    // Hard delete (physical)
    Task<int> ExecuteHardDeleteAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);

    // Bulk hard delete by entities
    Task BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    IQueryable<TEntity> QueryWithIgnoreQueryFilters(Expression<Func<TEntity, bool>>? expression = null);
    IQueryable<TEntity> QueryAsNoTracking(Expression<Func<TEntity, bool>>? expression = null);

    // Existing explicit soft delete (allows external user/time for batch scenarios)
    Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, string? userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> RestoreAsync(IQueryable<TEntity> queryable, string? userId, DateTime utcNow, CancellationToken cancellationToken = default);

    // Convenience overloads using injected context/time
    Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);
    Task<int> RestoreAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);

    IQueryable<T> GetEntitiesWithinBoundaries<T>(
       IQueryable<T> query,
       ViewportBounds? viewport = null,
       List<Guid>? regionIds = null,
       List<Guid>? provinceIds = null,
       List<Guid>? municipalityIds = null,
       List<Guid>? barangayIds = null) where T : IGeoLocatedEntity;
}
