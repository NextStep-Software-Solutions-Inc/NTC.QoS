using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IFrequencyAllocationRepository : IRepository<FrequencyAllocation>
{
    /// <summary>
    /// Returns a query with the full navigation graph eagerly loaded
    /// (Provider, UpLink/DownLink → Band, ServiceProvider → Provider/Service).
    /// Use only when callers need to materialize the object graph rather than project to a DTO.
    /// </summary>
    IQueryable<FrequencyAllocation> QueryWithIncludes(Expression<Func<FrequencyAllocation, bool>>? expression = null);
}
