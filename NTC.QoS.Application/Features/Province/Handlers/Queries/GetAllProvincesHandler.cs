using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;

public class GetAllProvincesHandler(IProvinceRepository provinceRepository, ICellRepository cellRepository, INAPRepository nAPRepository) : IRequestHandler<GetAllProvincesRequest, PaginatedResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly INAPRepository _nAPRepository = nAPRepository;

    public async Task<PaginatedResponse<ProvinceDto>> Handle(GetAllProvincesRequest request, CancellationToken cancellationToken)
    {
        var provincesQuery = _provinceRepository.Query();

        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        var allCellsQuery = _cellRepository.Query();
        var allNAPsQuery = _nAPRepository.Query();

        var provinceDtosQuery = provincesQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .OrderBy(p => p.Name)
            .Select(p => new ProvinceDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Code = p.Code,
                Area = p.Area,
                Region = new()
                {
                    Id = p.Region.Id,         
                    Name = p.Region.Name,    
                    Description = p.Region.Description,
                },
                MunicipalityCount = p.Municipalities.Count(),
                BarangayCount = p.Municipalities.Sum(m => m.Barangays.Count),
                ISPCount = p.Boundaries != null
                    ? allCellsQuery
                        .Where(cell => p.Boundaries.Intersects(cell.Location))
                        .Select(cell => cell.ServiceProviderId)
                        .Concat( // <--- Merge with ServiceProviderIds from NAPs
                            allNAPsQuery
                                .Where(nap => p.Boundaries.Intersects(nap.Location))
                                .Select(nap => nap.ServiceProviderId)
                        )
                        .Distinct() // <--- Get distinct IDs from the combined list
                        .Count()
                    : 0
            }); 

        return await _provinceRepository.PaginateAsync(provinceDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}