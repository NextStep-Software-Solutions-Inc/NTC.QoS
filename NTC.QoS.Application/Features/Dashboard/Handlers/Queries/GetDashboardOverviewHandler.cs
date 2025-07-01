using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var cellsQuery = _cellRepository.Query();

        if (request.StartDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt <= request.EndDate.Value);

        var verifiedCells = await cellsQuery.CountAsync(c => c.IsVerified, cancellationToken);
        var unverifiedCells = await cellsQuery.CountAsync(c => !c.IsVerified, cancellationToken);
        var totalCells = await cellsQuery.CountAsync(cancellationToken);

        var cellData = new CellsiteCountsDto
        {
            VerifiedCells = verifiedCells,
            UnverifiedCells = unverifiedCells,
            TotalCells = totalCells
        };

        var nAPsQuery = _napRepository.Query();

        if (request.StartDate.HasValue)
            nAPsQuery = nAPsQuery.Where(nap => nap.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            nAPsQuery = nAPsQuery.Where(nap => nap.CreatedAt <= request.EndDate.Value);

        var nAPsCount = await nAPsQuery.CountAsync(cancellationToken);

        var topRegionsByCellCount = await _geospatialQueryService.GetTopRegionsByCellCountAsync(request.Limit, request.StartDate, request.EndDate);

        var topProvincesInRegionByCellCount = await _geospatialQueryService.GetTopProvincesInRegionByCellCountAsync(request.RegionId, request.Limit, request.StartDate, request.EndDate);

        var topMunicipalitiesInProvinceByCellCount = await _geospatialQueryService.GetTopMunicipalitiesInProvinceByCellCountAsync(request.ProvinceId, request.Limit, request.StartDate, request.EndDate);

        var topBarangaysInMunicipalityByCellCount = await _geospatialQueryService.GetTopBarangaysInMunicipalityByCellCountAsync(request.MunicipalityId, request.Limit, request.StartDate, request.EndDate);

        return await Task.FromResult(new ApiResponse<DashboardOverviewDto>
        {
            Data = new()
            {
                CellsiteCounts = cellData,
                TotalNAPsCount = nAPsCount,
                TopRegionsByCellCount = topRegionsByCellCount,
                TopProvincesInRegionByCellCount = topProvincesInRegionByCellCount,
                TopMunicipalitiesInProvinceByCellCount = topMunicipalitiesInProvinceByCellCount,
                TopBarangaysInMunicipalityByCellCount = topBarangaysInMunicipalityByCellCount
            },
            Succeeded = true,
            Message = $"Dashboard data overview between ({request.StartDate}) to ({request.EndDate}) retrieved successfully."
        });
    }
}
