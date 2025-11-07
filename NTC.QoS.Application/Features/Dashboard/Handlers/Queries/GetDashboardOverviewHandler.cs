using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetDashboardOverviewHandler : IRequestHandler<GetDashboardOverviewRequest, ApiResponse<DashboardOverviewDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;

    public GetDashboardOverviewHandler(
        ICellRepository cellRepository, 
        INAPRepository napRepository, 
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
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

        var topRegionsByCellCount = await _regionRepository.GetTopRegionsByCellCountAsync(
            request.Limit, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);

        var topProvincesInRegionByCellCount = await _provinceRepository.GetTopProvincesByCellCountAsync(
            request.RegionId, 
            request.Limit, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);

        var topMunicipalitiesInProvinceByCellCount = await _municipalityRepository.GetTopMunicipalitiesByCellCountAsync(
            request.ProvinceId, 
            request.Limit, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);

        var topBarangaysInMunicipalityByCellCount = await _barangayRepository.GetTopBarangaysByCellCountAsync(
            request.MunicipalityId, 
            request.Limit, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);

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
