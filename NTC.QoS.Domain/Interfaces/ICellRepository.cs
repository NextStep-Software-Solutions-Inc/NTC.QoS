using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Domain.Interfaces;

public interface ICellRepository : IRepository<Cell>
{
    /// <summary>
    /// Gets cell sites grouped by service provider with optimized queries.
    /// </summary>
    Task<CellSitesByProviderResponseDto> GetCellSitesByProviderAsync(
   Guid? regionId = null,
        Guid? serviceProviderId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
     CancellationToken cancellationToken = default);
}
