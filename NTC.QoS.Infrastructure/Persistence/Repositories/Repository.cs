using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Infrastructure.Exceptions;
using System.Linq.Expressions;

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

    public async Task<int> CountAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        return await queryable.CountAsync(cancellationToken);
    }

    public void Add(TEntity entity) => _context.Add(entity);
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) => await _context.AddAsync(entity, cancellationToken);
    public void Update(TEntity entity) => _context.Update(entity);
    public void Delete(TEntity entity) => _context.Remove(entity);
    public void AddRange(IEnumerable<TEntity> entities) => _context.AddRange(entities);
    public async Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var bulkConfig = new EFCore.BulkExtensions.BulkConfig
        {
            BulkCopyTimeout = 600, // 10 minutes for large batches
            BatchSize = 500 // Optimize batch size for PostgreSQL
        };
        await _context.BulkInsertAsync(entities, bulkConfig, cancellationToken: cancellationToken);
    }
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

    public Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) => SoftDeleteAsync(queryable, GetUserContextService()?.GetCurrentUserId(), DateTime.UtcNow, cancellationToken);

    public Task<int> RestoreAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) => RestoreAsync(queryable, GetUserContextService()?.GetCurrentUserId(), DateTime.UtcNow, cancellationToken);

    /// <summary>
    /// Applies case-insensitive search filter across multiple properties.
    /// Uses EF.Functions.ILike for PostgreSQL case-insensitive pattern matching.
    /// </summary>
    /// <typeparam name="T">The entity type to search</typeparam>
    /// <param name="query">The queryable to filter</param>
    /// <param name="searchKeyword">The search keyword</param>
    /// <param name="propertySelectors">Property selectors to search across</param>
    /// <returns>Filtered queryable</returns>
    protected IQueryable<T> ApplySearch<T>(
        IQueryable<T> query,
        string? searchKeyword,
        params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrEmpty(searchKeyword) || propertySelectors == null || propertySelectors.Length == 0)
        {
            return query;
        }

        var searchPattern = $"%{searchKeyword}%";
        Expression<Func<T, bool>>? combinedPredicate = null;

        foreach (var propertySelector in propertySelectors)
        {
            // Create predicate: property != null && EF.Functions.Like(property, pattern)
            var parameter = propertySelector.Parameters[0];
            var propertyExpression = propertySelector.Body;

            // Null check: property != null
            var nullCheck = Expression.NotEqual(
                propertyExpression,
                Expression.Constant(null, typeof(string))
            );

            // EF.Functions.ILike(property, pattern) for case-insensitive search in PostgreSQL
            var iLikeMethod = typeof(NpgsqlDbFunctionsExtensions).GetMethod(
                nameof(NpgsqlDbFunctionsExtensions.ILike),
                new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

            var likeCall = Expression.Call(iLikeMethod,
                Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                propertyExpression,
                Expression.Constant(searchPattern)
                );

            // Combine: property != null && EF.Functions.Like(property, pattern)
            var condition = Expression.AndAlso(nullCheck, likeCall);
            var predicate = Expression.Lambda<Func<T, bool>>(condition, parameter);

            // Combine with OR
            combinedPredicate = combinedPredicate == null
                ? predicate
                : CombineWithOr(combinedPredicate, predicate);
        }

        return combinedPredicate != null ? query.Where(combinedPredicate) : query;
    }

    /// <summary>
    /// Combines two predicates with OR logic
    /// </summary>
    private static Expression<Func<T, bool>> CombineWithOr<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var body1 = new ParameterReplacer(parameter).Visit(expr1.Body);
        var body2 = new ParameterReplacer(parameter).Visit(expr2.Body);
        var combined = Expression.OrElse(body1!, body2!);
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// Helper class to replace parameters in expressions
    /// </summary>
    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        public ParameterReplacer(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _parameter;
        }
    }

    /// <summary>
    /// Filter by geographical bounds and region/province/municipality/barangay IDs.
    /// OPTIMIZED: Uses subquery approach to avoid correlated subqueries and leverage spatial indexes.
    /// </summary>
    /// <typeparam name="T">The entity type with geo-location</typeparam>
    /// <param name="query">The queryable to filter</param>
    /// <param name="viewport">The viewport boundaries</param>
    /// <param name="regionIds">The region IDs</param>
    /// <param name="provinceIds">The province IDs</param>
    /// <param name="municipalityIds">The municipality IDs</param>
    /// <param name="barangayIds">The barangay IDs</param>
    /// <returns>Filtered queryable</returns>
    public IQueryable<T> GetEntitiesWithinBoundaries<T>(
        IQueryable<T> query,
        ViewportBounds? viewport = null,
        List<Guid>? regionIds = null,
        List<Guid>? provinceIds = null,
        List<Guid>? municipalityIds = null,
        List<Guid>? barangayIds = null) where T : IGeoLocatedEntity
    {
        if (viewport is not null)
        {
            // Filter by viewport bounds
            query = query.Where(entity => entity.Location != null && entity.Location.Within(viewport.CreateEnvelopeFromViewport()));
        }

        // OPTIMIZED: Use proper subquery approach with Intersects instead of correlated subqueries
        if (regionIds is not null && regionIds.Count > 0)
        {
            var regionBoundariesQuery = _context.Regions
                .Where(r => regionIds.Contains(r.Id) && r.Boundaries != null)
                .Select(r => r.Boundaries);

            // Use Intersects for geography compatibility and proper spatial index usage
            query = query.Where(entity =>
                regionBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(entity.Location)));
        }

        if (provinceIds is not null && provinceIds.Count > 0)
        {
            var provinceBoundariesQuery = _context.Provinces
                .Where(p => provinceIds.Contains(p.Id) && p.Boundaries != null)
                .Select(p => p.Boundaries);

            query = query.Where(entity =>
                provinceBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(entity.Location)));
        }

        if (municipalityIds is not null && municipalityIds.Count > 0)
        {
            var municipalityBoundariesQuery = _context.Municipalities
                .Where(m => municipalityIds.Contains(m.Id) && m.Boundaries != null)
                .Select(m => m.Boundaries);

            query = query.Where(entity =>
                municipalityBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(entity.Location)));
        }

        if (barangayIds is not null && barangayIds.Count > 0)
        {
            var barangayBoundariesQuery = _context.Barangays
                .Where(b => barangayIds.Contains(b.Id) && b.Boundaries != null)
                .Select(b => b.Boundaries);

            query = query.Where(entity =>
            barangayBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(entity.Location)));
        }

        return query;
    }
}
