namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class DashboardOverviewDto
{
    public CellsiteCountsDto CellsiteCounts { get; set; } = new CellsiteCountsDto();
    public int TotalNAPsCount { get; set; }
    public List<GeoEntityCellCountDto> TopGeoEntitesByCellCount { get; set; } = [];
    public List<GeoEntityCellCountDto> TopChildGeoEntityByCellCount { get; set; } = [];
}
