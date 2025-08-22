using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Application.Services.Interface;

public interface IGeographicFilteringService
{
    Task<IQueryable<ServiceProvider>> ApplyGeographicFiltersAsync(
        IQueryable<ServiceProvider> query,
        List<string>? areas,
        List<string>? regions,
        CancellationToken cancellationToken);
}