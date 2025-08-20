using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class DashboardAggregateDTO
{
    public ApiResponse<CellsiteCountsDto>? TotalCellsitesCount { get; set; }
    public ApiResponse<int>? TotalNAPsCount { get; set; }
    public ApiResponse<List<GeoEntityCellCountDto>>? TopRegionsByCellCount { get; set; }
    public ApiResponse<List<GeoEntityCellCountDto>>? TopProvincesInRegionByCellCount { get; set; }
    public ApiResponse<List<GeoEntityCellCountDto>>? TopMunicipalitiesInProvinceByCellCount { get; set; }
    public ApiResponse<List<GeoEntityCellCountDto>>? TopBarangaysInMunicipalityByCellCount { get; set; }
    public ApiResponse<WirelessCoverageResponseDto>? WirelessCoverage { get; set; }
}
