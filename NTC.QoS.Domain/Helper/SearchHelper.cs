using System.Linq.Expressions;

namespace NTC.QoS.Domain.Helper;

public static class SearchHelper
{
    /// <summary>
    /// Builds a LINQ expression for searching a keyword across multiple nullable string properties of a given type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="keyword">The keyword to search for.</param>
    /// <param name="propertySelectors">An array of expressions that select the nullable string properties to search in.</param>
    /// <returns>An expression that can be used in a Where clause.</returns>
    public static Expression<Func<T, bool>> BuildNullableKeywordSearchExpression<T>(string keyword, params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrEmpty(keyword) || propertySelectors == null || propertySelectors.Length == 0)
        {
            return x => true; // Return a predicate that always evaluates to true (no filter).
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var propertySelector in propertySelectors)
        {
            var property = Expression.Invoke(propertySelector, parameter);

            // Check for null before calling Contains to avoid NullReferenceException
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));

            // Get the Contains method info
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) }) ?? throw new InvalidOperationException("The 'Contains' method was not found on type 'string'.");

            // Convert property value and keyword to lower case for case-insensitive search
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes) ?? throw new InvalidOperationException("The 'ToLower' method was not found on type 'string'.");
            var lowerProperty = Expression.Call(property, toLowerMethod);
            var lowerKeyword = keyword.ToLower();

            // Create the Contains expression.
            var containsExpression = Expression.Call(lowerProperty, containsMethod, Expression.Constant(lowerKeyword));

            // Combine null check and contains with AND.
            var safeContains = Expression.AndAlso(nullCheck, containsExpression);

            // Combine the current property's expression with the overall expression using OR.
            combinedExpression = combinedExpression == null ? safeContains : Expression.OrElse(combinedExpression, safeContains);
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression ?? Expression.Constant(true), parameter);
    }

}
