using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class DashboardAggregateDTO
{
    public CellsiteCountsDto? TotalCellsitesCount { get; set; }
    public int? TotalNAPsCount { get; set; }
    public List<TopRegionsByCellCountDto>? TopRegionsByCellCount { get; set; }
    public List<TopProvincesInRegionByCellCountDto>? TopProvincesInRegionByCellCount { get; set; }
    public List<TopMunicipalitiesInProvinceByCellCountDto>? TopMunicipalitiesInProvinceByCellCount { get; set; }
    public List<TopBarangaysInMunicipalitiesByCellCountDto>? TopBarangaysInMunicipalityByCellCount { get; set; }
}