using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Specifications
{
    public class BaseSpecification<T> : ISpecification<T>
    {
        public BaseSpecification()
        {
            Includes = new List<Expression<Func<T, object>>>();
            ThenIncludes = new List<Tuple<Expression<Func<T, object>>, Expression<Func<object, object>>>>();
            Criteria = _ => true; // Default criteria to avoid null
        }

        public BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
            Includes = new List<Expression<Func<T, object>>>();
            ThenIncludes = new List<Tuple<Expression<Func<T, object>>, Expression<Func<object, object>>>>();
        }

        public Expression<Func<T, bool>> Criteria { get; protected set; }
        public List<Expression<Func<T, object>>> Includes { get; }
        public List<Tuple<Expression<Func<T, object>>, Expression<Func<object, object>>>> ThenIncludes { get; }
        public bool IsNoTracking { get; protected set; }


        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected virtual void AddThenInclude<TProperty>(Expression<Func<T, object>> includeExpression, Expression<Func<object, object>> thenIncludeExpression)
        {
            ThenIncludes.Add(new Tuple<Expression<Func<T, object>>, Expression<Func<object, object>>>(includeExpression, thenIncludeExpression));
        }

        protected virtual void ApplyCriteria(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        protected virtual void ApplyNoTracking()
        {
            IsNoTracking = true;
        }
    }
}
