using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Database-optimized search helper that uses EF.Functions.ILike for case-insensitive text search
/// This implementation provides better performance with database indexes compared to ToLower().Contains()
/// </summary>
public static class DatabaseSearchHelper
{
    /// <summary>
    /// Builds a database-optimized LINQ expression for searching a keyword across multiple nullable string properties.
    /// Uses EF.Functions.ILike() which translates to PostgreSQL ILIKE operator for efficient case-insensitive search.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="keyword">The keyword to search for.</param>
    /// <param name="propertySelectors">An array of expressions that select the nullable string properties to search in.</param>
    /// <returns>An expression that can be used in a Where clause with EF Core.</returns>
    /// <remarks>
    /// This method is optimized for database queries and should be used instead of BuildNullableKeywordSearchExpression
    /// when working with EF Core queryables. It enables database index usage and better performance.
    /// </remarks>
    public static Expression<Func<T, bool>> BuildDatabaseSearchExpression<T>(
        string keyword, 
        params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrEmpty(keyword) || propertySelectors == null || propertySelectors.Length == 0)
        {
            return x => true; // Return a predicate that always evaluates to true (no filter).
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        // Get EF.Functions.ILike method
        var efFunctionsType = typeof(EF);
        var functionsProperty = efFunctionsType.GetProperty("Functions", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("EF.Functions property not found.");
        
        var iLikeMethod = typeof(DbFunctionsExtensions).GetMethod(
            "ILike",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(DbFunctions), typeof(string), typeof(string) },
            null)
            ?? throw new InvalidOperationException("The 'ILike' method was not found on DbFunctionsExtensions.");

        // Pattern for partial matching: %keyword%
        var searchPattern = $"%{keyword}%";

        foreach (var propertySelector in propertySelectors)
        {
            var property = Expression.Invoke(propertySelector, parameter);

            // Check for null before calling ILike to avoid NullReferenceException
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));

            // Create EF.Functions.ILike(property, "%keyword%") expression
            var functionsExpression = Expression.Property(null, functionsProperty);
            var iLikeExpression = Expression.Call(
                iLikeMethod,
                functionsExpression,
                property,
                Expression.Constant(searchPattern));

            // Combine null check and ILike with AND
            var safeILike = Expression.AndAlso(nullCheck, iLikeExpression);

            // Combine the current property's expression with the overall expression using OR
            combinedExpression = combinedExpression == null ? safeILike : Expression.OrElse(combinedExpression, safeILike);
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression ?? Expression.Constant(true), parameter);
    }

    /// <summary>
    /// Builds a database-optimized LINQ expression for exact case-insensitive matching across multiple properties.
    /// Uses EF.Functions.ILike() without wildcards for exact matching.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="keyword">The keyword to match exactly (case-insensitive).</param>
    /// <param name="propertySelectors">An array of expressions that select the nullable string properties to search in.</param>
    /// <returns>An expression that can be used in a Where clause with EF Core.</returns>
    public static Expression<Func<T, bool>> BuildDatabaseExactMatchExpression<T>(
        string keyword, 
        params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrEmpty(keyword) || propertySelectors == null || propertySelectors.Length == 0)
        {
            return x => false; // Return a predicate that always evaluates to false (no matches).
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        // Get EF.Functions.ILike method
        var efFunctionsType = typeof(EF);
        var functionsProperty = efFunctionsType.GetProperty("Functions", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("EF.Functions property not found.");
        
        var iLikeMethod = typeof(DbFunctionsExtensions).GetMethod(
            "ILike",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(DbFunctions), typeof(string), typeof(string) },
            null)
            ?? throw new InvalidOperationException("The 'ILike' method was not found on DbFunctionsExtensions.");

        foreach (var propertySelector in propertySelectors)
        {
            var property = Expression.Invoke(propertySelector, parameter);

            // Check for null before calling ILike
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));

            // Create EF.Functions.ILike(property, "keyword") expression (exact match, case-insensitive)
            var functionsExpression = Expression.Property(null, functionsProperty);
            var iLikeExpression = Expression.Call(
                iLikeMethod,
                functionsExpression,
                property,
                Expression.Constant(keyword));

            // Combine null check and ILike with AND
            var safeILike = Expression.AndAlso(nullCheck, iLikeExpression);

            // Combine the current property's expression with the overall expression using OR
            combinedExpression = combinedExpression == null ? safeILike : Expression.OrElse(combinedExpression, safeILike);
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression ?? Expression.Constant(true), parameter);
    }

    /// <summary>
    /// Builds a database-optimized LINQ expression for prefix matching (starts with) across multiple properties.
    /// Uses EF.Functions.ILike() with suffix wildcard for efficient prefix search.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="prefix">The prefix to search for.</param>
    /// <param name="propertySelectors">An array of expressions that select the nullable string properties to search in.</param>
    /// <returns>An expression that can be used in a Where clause with EF Core.</returns>
    public static Expression<Func<T, bool>> BuildDatabasePrefixSearchExpression<T>(
        string prefix, 
        params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrEmpty(prefix) || propertySelectors == null || propertySelectors.Length == 0)
        {
            return x => true;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        var efFunctionsType = typeof(EF);
        var functionsProperty = efFunctionsType.GetProperty("Functions", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("EF.Functions property not found.");
        
        var iLikeMethod = typeof(DbFunctionsExtensions).GetMethod(
            "ILike",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(DbFunctions), typeof(string), typeof(string) },
            null)
            ?? throw new InvalidOperationException("The 'ILike' method was not found on DbFunctionsExtensions.");

        // Pattern for prefix matching: prefix%
        var searchPattern = $"{prefix}%";

        foreach (var propertySelector in propertySelectors)
        {
            var property = Expression.Invoke(propertySelector, parameter);
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));

            var functionsExpression = Expression.Property(null, functionsProperty);
            var iLikeExpression = Expression.Call(
                iLikeMethod,
                functionsExpression,
                property,
                Expression.Constant(searchPattern));

            var safeILike = Expression.AndAlso(nullCheck, iLikeExpression);
            combinedExpression = combinedExpression == null ? safeILike : Expression.OrElse(combinedExpression, safeILike);
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression ?? Expression.Constant(true), parameter);
    }

    /// <summary>
    /// Builds a database-optimized LINQ expression for suffix matching (ends with) across multiple properties.
    /// Uses EF.Functions.ILike() with prefix wildcard for efficient suffix search.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="suffix">The suffix to search for.</param>
    /// <param name="propertySelectors">An array of expressions that select the nullable string properties to search in.</param>
    /// <returns>An expression that can be used in a Where clause with EF Core.</returns>
    public static Expression<Func<T, bool>> BuildDatabaseSuffixSearchExpression<T>(
        string suffix, 
        params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrEmpty(suffix) || propertySelectors == null || propertySelectors.Length == 0)
        {
            return x => true;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        var efFunctionsType = typeof(EF);
        var functionsProperty = efFunctionsType.GetProperty("Functions", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("EF.Functions property not found.");
        
        var iLikeMethod = typeof(DbFunctionsExtensions).GetMethod(
            "ILike",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(DbFunctions), typeof(string), typeof(string) },
            null)
            ?? throw new InvalidOperationException("The 'ILike' method was not found on DbFunctionsExtensions.");

        // Pattern for suffix matching: %suffix
        var searchPattern = $"%{suffix}";

        foreach (var propertySelector in propertySelectors)
        {
            var property = Expression.Invoke(propertySelector, parameter);
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));

            var functionsExpression = Expression.Property(null, functionsProperty);
            var iLikeExpression = Expression.Call(
                iLikeMethod,
                functionsExpression,
                property,
                Expression.Constant(searchPattern));

            var safeILike = Expression.AndAlso(nullCheck, iLikeExpression);
            combinedExpression = combinedExpression == null ? safeILike : Expression.OrElse(combinedExpression, safeILike);
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression ?? Expression.Constant(true), parameter);
    }
}
