using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetDashboardOverviewHandler : IRequestHandler<GetDashboardOverviewRequest, ApiResponse<DashboardOverviewDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly GeospatialQueryService _geospatialQueryService;

    public GetDashboardOverviewHandler(ICellRepository cellRepository, INAPRepository napRepository, GeospatialQueryService geospatialQueryService)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _geospatialQueryService = geospatialQueryService;
    }

    public async Task<ApiResponse<DashboardOverviewDto>> Handle(GetDashboardOverviewRequest request, CancellationToken cancellationToken)
    {
        var cells = _cellRepository.Query(c => !c.IsDeleted).ToList();

        var verifiedCells = cells.Count(c => c.IsVerified && c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);
        var unverifiedCells = cells.Count(c => !c.IsVerified && c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);
        var totalCells = cells.Count(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);

        var cellData = new CellsiteCountsDto
        {
            VerifiedCells = verifiedCells,
            UnverifiedCells = unverifiedCells,
            TotalCells = totalCells
        };

        var nAPsCount = _napRepository.Query(c => !c.IsDeleted &&
            c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate).Count();

        var topRegionsByCellCount = await _geospatialQueryService.GetTopGeoEntitiesByCellCountAsync<Domain.Entities.Region>(request.Limit, request.StartDate, request.EndDate);

        var topProvincesInRegionByCellCount = await _geospatialQueryService.GetTopChildEntitiesByCellCountAsync<Domain.Entities.Region, Domain.Entities.Province>(request.RegionId, "RegionId", request.Limit, request.StartDate, request.EndDate);

        return await Task.FromResult(new ApiResponse<DashboardOverviewDto>
        {
            Data = new()
            {
                CellsiteCounts = cellData,
                TotalNAPsCount = nAPsCount,
                TopRegionsByCellCount = topRegionsByCellCount,
                TopProvincesInRegionByCellCount = topProvincesInRegionByCellCount
            },
            Succeeded = true,
            Message = $"Dashboard data overview between ({request.StartDate}) to ({request.EndDate}) retrieved successfully."
        });
    }
}
