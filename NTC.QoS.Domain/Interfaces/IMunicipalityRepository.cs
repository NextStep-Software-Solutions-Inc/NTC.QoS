using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Interfaces;

public interface IMunicipalityRepository : IRepository<Municipality>
{
    Task<Municipality?> GetMunicipalityByLocationAsync(Point location, CancellationToken cancellationToken = default);
    Task<(Guid? Id, string Name)> GetMunicipalityIdAndNameByLocationAsync(Point location, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets top municipalities by cell count with optimized spatial queries.
    /// </summary>
 Task<List<GeoEntityCellCountDto>> GetTopMunicipalitiesByCellCountAsync(
      Guid? provinceId,
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}