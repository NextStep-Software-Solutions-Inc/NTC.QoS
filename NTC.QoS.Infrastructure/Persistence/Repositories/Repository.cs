using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Exceptions;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContext;
    private readonly ITimeProvider _timeProvider;

    public Repository(ApplicationDbContext context, IUserContextService? userContext = null, ITimeProvider? timeProvider = null)
    {
        _context = context;
        var serviceProvider = context.GetService<IServiceProvider>();
        _userContext = userContext ?? serviceProvider.GetRequiredService<IUserContextService>();
        _timeProvider = timeProvider ?? serviceProvider.GetRequiredService<ITimeProvider>();
    }

    [Obsolete("FindAsync is deprecated. Use the Query method with appropriate filters instead then call FirstOrDefaultAsync().")]
    public virtual async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        => await _context.Set<TEntity>().FirstOrDefaultAsync(expression, cancellationToken);

    [Obsolete("GetByIdAsync is deprecated. Use the Query method with appropriate filters instead then call FirstOrDefaultAsync().")]
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);

    [Obsolete("ListAsync is deprecated. Use the Query method with appropriate filters instead then call ToListAsync or PaginateAsync.")]
    public virtual async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();
        if (expression != null) query = query.Where(expression);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? expression = null)
    {
        var query = _context.Set<TEntity>().AsQueryable();
        if (expression != null) query = query.Where(expression);
        return query;
    }

    public async Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
        => await queryable.ToListAsync(cancellationToken);

    public void Add(TEntity entity) => _context.Add(entity);
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) => await _context.AddAsync(entity, cancellationToken);
    public void Update(TEntity entity) => _context.Update(entity);
    public void Delete(TEntity entity) => _context.Remove(entity);
    public void AddRange(IEnumerable<TEntity> entities) => _context.AddRange(entities);
    public async Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => await _context.BulkInsertAsync(entities, cancellationToken: cancellationToken);
    public void RemoveRange(IEnumerable<TEntity> entities) => _context.RemoveRange(entities);
    public async Task BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => await _context.BulkDeleteAsync(entities, cancellationToken: cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { await _context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { throw DatabaseExceptionHandler.HandleForeignKeyConstraintViolation(ex); }
    }

    public async Task<TResult?> FirstOrDefaultAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        return await queryable.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<TResult>> PaginateAsync<TResult>(IQueryable<TResult> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1.");
        var totalCount = await queryable.CountAsync(cancellationToken);
        var data = await queryable.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginatedResponse<TResult>(data, pageNumber, pageSize, totalCount);
    }

    [Obsolete("Use ExecuteHardDeleteAsync or SoftDeleteAsync.")]
    public async Task<int> ExecuteDeleteAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
        => await ExecuteHardDeleteAsync(queryable, cancellationToken);

    public async Task<int> ExecuteHardDeleteAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        return await queryable.ExecuteDeleteAsync(cancellationToken);
    }

    public IQueryable<TEntity> QueryWithIgnoreQueryFilters(Expression<Func<TEntity, bool>>? expression = null)
    {
        var query = _context.Set<TEntity>().IgnoreQueryFilters().AsQueryable();
        if (expression != null) query = query.Where(expression);
        return query;
    }

    public IQueryable<TEntity> QueryAsNoTracking(Expression<Func<TEntity, bool>>? expression = null)
    {
        var query = _context.Set<TEntity>().AsNoTracking();
        if (expression != null) query = query.Where(expression);
        return query;
    }

    public async Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, string? userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        var list = await queryable.ToListAsync(cancellationToken);
        if (list.Count == 0) return 0;
        foreach (var entity in list.Where(e => !e.IsDeleted))
        {
            entity.IsDeleted = true;
            entity.DeletedAt = utcNow;
            entity.DeletedBy = userId ?? "System";
            entity.UpdatedAt = utcNow;
            entity.UpdatedBy = userId ?? "System";
        }
        await _context.SaveChangesAsync(cancellationToken);
        return list.Count(e => e.IsDeleted);
    }

    public async Task<int> RestoreAsync(IQueryable<TEntity> queryable, string? userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        var list = await queryable.ToListAsync(cancellationToken);
        if (list.Count == 0) return 0;
        foreach (var entity in list.Where(e => e.IsDeleted))
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedBy = string.Empty; // Use empty string instead of null
            entity.UpdatedAt = utcNow;
            entity.UpdatedBy = userId ?? "System";
        }
        await _context.SaveChangesAsync(cancellationToken);
        return list.Count(e => !e.IsDeleted);
    }

    public Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default)
        => SoftDeleteAsync(queryable, _userContext.GetCurrentUserId(), _timeProvider.UtcNow, cancellationToken);

    public Task<int> RestoreAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default)
        => RestoreAsync(queryable, _userContext.GetCurrentUserId(), _timeProvider.UtcNow, cancellationToken);
}
