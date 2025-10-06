using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Interfaces;

public interface IConnectivityAssessmentRepository : IRepository<ConnectivityAssessment>
{
    Task<ConnectivityAssessment?> GetByLocationAsync(Guid locationId, LocationTypeEnum locationType, CancellationToken cancellationToken = default);
    Task<List<ConnectivityAssessment>> GetByLocationIdsAsync(List<Guid> locationIds, LocationTypeEnum locationType, CancellationToken cancellationToken = default);
    Task<List<ConnectivityAssessment>> GetByConnectivityStatusAsync(ConnectivityStatusEnum status, CancellationToken cancellationToken = default);
    Task<bool> ExistsForLocationAsync(Guid locationId, LocationTypeEnum locationType, CancellationToken cancellationToken = default);
}