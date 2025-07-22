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
        var provincesQuery = _provinceRepository.Query();

        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        var provinceDtosQuery = provincesQuery
            .OrderBy(p => p.Name)
            .Select(static p => new ProvinceDto
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
                BarangayCount = p.Municipalities.Sum(m => m.Barangays.Count())
            });

        return await _provinceRepository.PaginateAsync(provinceDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}