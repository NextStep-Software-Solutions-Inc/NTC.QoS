using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class DashboardAggregateDTO
{
    public class DashboardAggregateDto
    {
        public ApiResponse<CellsiteCountsDto>? TotalCellsiteCount { get; set; }
        public ApiResponse<int>? TotalNAPCount { get; set; }
        public ApiResponse<List<GeoEntityCellCountDto>>? TopRegions { get; set; }
        public ApiResponse<List<GeoEntityCellCountDto>>? TopProvincesInRegion { get; set; }
        public ApiResponse<DashboardOverviewDto>? Overview { get; set; }
    }
}
