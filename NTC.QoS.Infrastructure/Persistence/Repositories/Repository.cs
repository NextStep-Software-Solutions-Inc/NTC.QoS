using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Exceptions;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

public class Repository<TEntity>(ApplicationDbContext context) : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext _context = context;

    public virtual async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>().FirstOrDefaultAsync(expression, cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().Where(e => !EF.Property<bool>(e, "IsDeleted")); 

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public virtual async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().Where(e => !EF.Property<bool>(e, "IsDeleted"));

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public virtual IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? expression = null)
    {
        var query = _context.Set<TEntity>().Where(e => !EF.Property<bool>(e, "IsDeleted"));

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }

    public async Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
    {
        return await queryable.ToListAsync(cancellationToken);
    }

    public void Add(TEntity entity)
    {
         _context.Add(entity);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _context.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        _context.Remove(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

        }
        catch (DbUpdateException ex)
        {
            throw DatabaseExceptionHandler.HandleForeignKeyConstraintViolation(ex);
        }
    }

}