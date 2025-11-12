using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;

public class GetAllProvincesHandler(IProvinceRepository provinceRepository) : IRequestHandler<GetAllProvincesRequest, PaginatedResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;

    public async Task<PaginatedResponse<ProvinceDto>> Handle(GetAllProvincesRequest request, CancellationToken cancellationToken)
    {
        // Step 1: Get paginated provinces WITHOUT ISP counts (to avoid N+1 queries and boundary materialization)
        var provincesQuery = _provinceRepository.QueryAsNoTracking();
        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        var provinceProjection = provincesQuery
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
                    Id = p.Region!.Id,
                    Name = p.Region.Name,
                    Description = p.Region.Description
                },
                BarangayCount = p.Municipalities!.SelectMany(m => m.Barangays!).Count(),
                MunicipalityCount = p.Municipalities!.Count(),
                ISPCount = 0 // Placeholder, will be calculated below
            });

        return await _provinceRepository.PaginateAsync(provinceProjection, request.PageNumber, request.PageSize, cancellationToken);

        // Step 2: Get province IDs for current page
        //var provinceIds = paginatedProvinces.Data?.Select(p => p.Id).ToList() ?? [];

        // Step 3: Compute ISP counts using database subqueries (NO boundary or location materialization)
        //var ispCounts = await _provinceRepository.GetIspCountsForProvincesAsync(provinceIds, cancellationToken);

        // Step 4: Combine results
        //var provinceDtos = paginatedProvinces.Data?.Select(p => new ProvinceDto
        //{
        //    Id = p.Id,
        //    Name = p.Name,
        //    Description = p.Description,
        //    Code = p.Code,
        //    Area = p.Area,
        //    Region = p.Region,
        //    BarangayCount = p.BarangayCount,
        //    MunicipalityCount = p.MunicipalityCount,
        //    ISPCount = 0
        //    //ISPCount = ispCounts.GetValueOrDefault(p.Id, 0)
        //}).ToList() ?? [];

        //return new PaginatedResponse<ProvinceDto>(provinceDtos, paginatedProvinces.PageNumber, paginatedProvinces.PageSize, paginatedProvinces.TotalRows);
    }
}