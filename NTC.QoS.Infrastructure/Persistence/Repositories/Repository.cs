using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Exceptions;
using System.Linq.Expressions;
using System.Reflection;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    private IUserContextService? _userContextService;
    private bool _userContextResolutionAttempted = false;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the user context service by resolving it from the DbContext's service provider when needed
    /// </summary>
    private IUserContextService? GetUserContextService()
    {
        // Only attempt resolution once
        if (!_userContextResolutionAttempted)
        {
            _userContextResolutionAttempted = true;
            
            try
            {
                var serviceProvider = _context.GetService<IServiceProvider>();
                _userContextService = serviceProvider?.GetService<IUserContextService>();
            }
            catch
            {
                // If service resolution fails, _userContextService will remain null
                // The methods that need it will handle this gracefully
                _userContextService = null;
            }
        }

        return _userContextService;
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

    /// <summary>
    /// Checks if the query has an OrderBy clause by examining the query provider
    /// </summary>
    private static bool HasOrderBy<TResult>(IQueryable<TResult> queryable)
    {
        // Check if the expression contains OrderBy, OrderByDescending, ThenBy, or ThenByDescending
        var expression = queryable.Expression;
        return HasOrderByInExpression(expression);
    }

    private static bool HasOrderByInExpression(Expression expression)
    {
        if (expression is MethodCallExpression methodCall)
        {
            // Check if this method call is an ordering method
            if (methodCall.Method.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
            {
                return true;
            }

            // Recursively check arguments for nested ordering
            foreach (var argument in methodCall.Arguments)
            {
                if (HasOrderByInExpression(argument))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Ensures the query has consistent ordering for pagination. If no OrderBy exists, adds a default one.
    /// </summary>
    private static IQueryable<TResult> EnsureOrdered<TResult>(IQueryable<TResult> queryable)
    {
        if (HasOrderBy(queryable))
        {
            return queryable;
        }

        // Try to find an Id property for consistent ordering
        var resultType = typeof(TResult);
        var idProperty = resultType.GetProperty("Id");
        
        if (idProperty != null)
        {
            // Create expression: x => x.Id
            var parameter = Expression.Parameter(resultType, "x");
            var propertyAccess = Expression.Property(parameter, idProperty);
            var lambda = Expression.Lambda(propertyAccess, parameter);
            
            // Call OrderBy method
            var orderByMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2)
                .MakeGenericMethod(resultType, idProperty.PropertyType);
                
            return (IQueryable<TResult>)orderByMethod.Invoke(null, new object[] { queryable, lambda })!;
        }

        // Fallback: try to order by the first property available
        var firstProperty = resultType.GetProperties().FirstOrDefault();
        if (firstProperty != null)
        {
            var parameter = Expression.Parameter(resultType, "x");
            var propertyAccess = Expression.Property(parameter, firstProperty);
            var lambda = Expression.Lambda(propertyAccess, parameter);
            
            var orderByMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2)
                .MakeGenericMethod(resultType, firstProperty.PropertyType);
                
            return (IQueryable<TResult>)orderByMethod.Invoke(null, new object[] { queryable, lambda })!;
        }

        // If no properties found, return original query (this should be rare)
        return queryable;
    }

    public async Task<PaginatedResponse<TResult>> PaginateAsync<TResult>(IQueryable<TResult> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1.");
        
        // Ensure the query has consistent ordering before pagination
        var orderedQuery = EnsureOrdered(queryable);
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        var data = await orderedQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
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

    public virtual IQueryable<TEntity> QueryAsNoTracking(Expression<Func<TEntity, bool>>? expression = null)
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
            entity.DeletedBy = string.Empty;
            entity.UpdatedAt = utcNow;
            entity.UpdatedBy = userId ?? "System";
        }

        return list.Count(e => !e.IsDeleted);
    }

    public Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default)
        => SoftDeleteAsync(queryable, GetUserContextService()?.GetCurrentUserId(), DateTime.UtcNow, cancellationToken);

    public Task<int> RestoreAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default)
        => RestoreAsync(queryable, GetUserContextService()?.GetCurrentUserId(), DateTime.UtcNow, cancellationToken);
}
