using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Queries;

/// <summary>
/// Handler for viewport-based Cellular Coverage queries (v2 API).
/// OPTIMIZED FOR MILLIONS OF DATA POINTS WITH LIMITED RESOURCES.
/// </summary>
public class GetAllCellularCoveragesViewportHandler : IRequestHandler<GetAllCellularCoveragesViewportRequest, PaginatedResponse<CellularCoverageViewportDto>>
{
    private readonly ICellularCoverageRepository _coverageRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;

    public GetAllCellularCoveragesViewportHandler(
        ICellularCoverageRepository coverageRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository)
    {
        _coverageRepository = coverageRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
    }

    public async Task<PaginatedResponse<CellularCoverageViewportDto>> Handle(GetAllCellularCoveragesViewportRequest request, CancellationToken cancellationToken)
    {
        if (!request.Viewport.IsValidForLimitedResources(out var validationError))
        {
            return new PaginatedResponse<CellularCoverageViewportDto>([], request.PageNumber, request.PageSize, 0)
            {
                Succeeded = false,
                Message = validationError ?? "Invalid viewport bounds"
            };
        }

        var maxAllowedPageSize = request.Viewport.GetRecommendedMaxResults();
        var effectivePageSize = Math.Min(request.PageSize, maxAllowedPageSize);

        var coverageQuery = _coverageRepository.Query().AsNoTracking();
        coverageQuery = coverageQuery.ApplyViewportFilter(request.Viewport);

        if (request.ServiceProviderIds != null && request.ServiceProviderIds.Count > 0)
        {
            coverageQuery = coverageQuery.Where(c => request.ServiceProviderIds.Contains(c.Network.ServiceProviderId));
        }

        if (request.BarangayIds is { Count: > 0 })
            coverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.BarangayIds, _barangayRepository, coverageQuery, cancellationToken);
        else if (request.MunicipalityIds is { Count: > 0 })
            coverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.MunicipalityIds, _municipalityRepository, coverageQuery, cancellationToken);
        else if (request.ProvinceIds is { Count: > 0 })
            coverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.ProvinceIds, _provinceRepository, coverageQuery, cancellationToken);
        else if (request.RegionIds is { Count: > 0 })
            coverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.RegionIds, _regionRepository, coverageQuery, cancellationToken);

        coverageQuery = coverageQuery.OrderBy(c => c.Id);

        var coverageViewportDtoQuery = coverageQuery.Select(c => new CellularCoverageViewportDto
        {
            ISP = c.Network.ServiceProvider.Provider.Name,
            ISPColor = c.Network.ServiceProvider.Provider.Color,
            Lat = c.Location.Coordinate.Y,
            Lng = c.Location.Coordinate.X,
            SignalStrength = c.SignalStrength
        });

        return await _coverageRepository.PaginateAsync(coverageViewportDtoQuery, request.PageNumber, effectivePageSize, cancellationToken);
    }
}
