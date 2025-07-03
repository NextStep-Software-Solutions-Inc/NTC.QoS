using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Interfaces;

public interface IGeospatialQueryService
{
    Task<List<GeoEntityCellCountDto>> GetTopRegionsByCellCountAsync(int limit, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<GeoEntityCellCountDto>> GetTopProvincesInRegionByCellCountAsync(Guid? regionId, int limit, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<GeoEntityCellCountDto>> GetTopMunicipalitiesInProvinceByCellCountAsync(Guid? provinceId, int limit, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<GeoEntityCellCountDto>> GetTopBarangaysInMunicipalityByCellCountAsync(Guid? municipalityId, int limit, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<GeoEntityCellCountDto>> GetTopGeoEntitiesByCellCountAsync<TEntity>(int limit, DateTime? startDate = null, DateTime? endDate = null) where TEntity : BaseEntity, new();
    Task<List<GeoEntityCellCountDto>> GetTopChildEntitiesByCellCountAsync<TParent, TChild>(Guid? parentId, string parentIdPropertyName, int limit, DateTime? startDate = null, DateTime? endDate = null) where TParent : BaseEntity where TChild : BaseEntity;
}