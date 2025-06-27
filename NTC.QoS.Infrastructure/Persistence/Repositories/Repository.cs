using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Exceptions;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

public class Repository<TEntity>(ApplicationDbContext context) : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext _context = context;

    [Obsolete("FindAsync is deprecated. Use the Query method with appropriate filters instead then call FirstOrDefaultAsync().")]
    public virtual async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>().FirstOrDefaultAsync(expression, cancellationToken);
    }

    [Obsolete("GetByIdAsync is deprecated. Use the Query method with appropriate filters instead then call FirstOrDefaultAsync().")]
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>(); 

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    
    [Obsolete("ListAsync is deprecated. Use the Query method with appropriate filters instead then call ToListAsync or PaginateAsync.")]
    public virtual async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }


    public virtual IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? expression = null)
    {
        var query = _context.Set<TEntity>().AsQueryable();

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

    public async Task<TResult?> FirstOrDefaultAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);

        return await queryable.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<TResult>> PaginateAsync<TResult>(
        IQueryable<TResult> queryable,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1.");
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var paginatedData = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<TResult>(paginatedData, pageNumber, pageSize, totalCount);
    }


    public void AddRange(IEnumerable<TEntity> entities)
    {
        _context.AddRange(entities);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        _context.RemoveRange(entities);
    }

    public async Task<int> ExecuteDeleteAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default) 
    {
        ArgumentNullException.ThrowIfNull(queryable);
        return await queryable.ExecuteDeleteAsync(cancellationToken);
    }
}