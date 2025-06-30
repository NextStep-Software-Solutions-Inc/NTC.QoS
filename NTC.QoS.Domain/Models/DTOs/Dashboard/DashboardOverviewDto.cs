using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class DashboardOverviewDto
{
    public CellsiteCountsDto CellsiteCounts { get; set; } = new CellsiteCountsDto();
    public int TotalNAPsCount { get; set; }
    public List<GeoEntityCellCountDto> TopRegionsByCellCount { get; set; } = [];
    public List<GeoEntityCellCountDto> TopProvincesInRegionByCellCount { get; set; } = [];
    public List<GeoEntityCellCountDto> TopMunicipalitiesInProvinceByCellCount { get; set; } = [];
    public List<GeoEntityCellCountDto> TopBarangaysInMunicipalityByCellCount { get; set; } = [];
}
