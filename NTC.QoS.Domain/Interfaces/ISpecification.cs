using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<Tuple<Expression<Func<T, object>>, Expression<Func<object, object>>>> ThenIncludes { get; }
    bool IsNoTracking { get; } // Add this for no-tracking queries
}
