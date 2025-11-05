using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Interfaces;

public interface IConnectivityAssessmentRepository : IRepository<ConnectivityAssessment>
{
    Task<List<ConnectivityAssessment>> GetByLocationIdsAsync(List<Guid> locationIds, LocationTypeEnum locationType, CancellationToken cancellationToken = default);
    Task<ConnectivityAssessment?> GetByLocationAsync(Guid locationId, LocationTypeEnum locationType, CancellationToken cancellationToken = default);
    Task<ConnectivityAssessment?> GetByLocationIdAsync(Guid locationId, LocationTypeEnum locationType, CancellationToken cancellationToken = default);
    Task<List<ConnectivityAssessment>> GetByConnectivityStatusAsync(ConnectivityStatusEnum status, CancellationToken cancellationToken = default);
    Task<bool> ExistsForLocationAsync(Guid locationId, LocationTypeEnum locationType, CancellationToken cancellationToken = default);
    Task<List<ConnectivityAssessmentProvider>> GetProvidersByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task DeleteProvidersByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task AddProviderAsync(ConnectivityAssessmentProvider provider, CancellationToken cancellationToken = default);
    Task AddProvidersRangeAsync(List<ConnectivityAssessmentProvider> providers, CancellationToken cancellationToken = default);
}
