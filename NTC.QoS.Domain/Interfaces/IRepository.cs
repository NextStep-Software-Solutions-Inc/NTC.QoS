using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}