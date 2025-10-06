using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IWiFiHotspotRepository : IRepository<WiFiHotspot>
{
    Task<List<WiFiHotspot>> GetActiveHotspotsAsync(CancellationToken cancellationToken = default);
    Task<List<WiFiHotspot>> GetPublicHotspotsAsync(CancellationToken cancellationToken = default);
    Task<List<WiFiHotspot>> GetHotspotsByServiceProviderAsync(Guid serviceProviderId, CancellationToken cancellationToken = default);
}