using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Defines a generic interface for a repository pattern.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default);
    Task<TEntity> GetByIdAsync(Guid id, ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default);
    Task<List<TEntity>> ListAsync(ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync();
}