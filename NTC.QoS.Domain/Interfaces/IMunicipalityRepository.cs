using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IMunicipalityRepository : IRepository<Municipality>
{
    Task<Municipality?> GetMunicipalityByLocationAsync(Point location, CancellationToken cancellationToken = default);
    Task<(Guid? Id, string Name)> GetMunicipalityIdAndNameByLocationAsync(Point location, CancellationToken cancellationToken = default);
}