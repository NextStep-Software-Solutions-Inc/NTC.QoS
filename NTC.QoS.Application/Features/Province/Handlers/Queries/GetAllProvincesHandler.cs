using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;

public class GetAllProvincesHandler(IProvinceRepository provinceRepository) : IRequestHandler<GetAllProvincesRequest, PaginatedResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;

    public async Task<PaginatedResponse<ProvinceDto>> Handle(GetAllProvincesRequest request, CancellationToken cancellationToken)
    {
        var provincesQuery = _provinceRepository.Query(p => !p.IsDeleted);

        if (request.RegionId.HasValue)
        {
            provincesQuery = provincesQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        var provinceDtosQuery = provincesQuery.Select(static p => new ProvinceDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Region = new RegionDto
            {
                Id = p.Region.Id,         
                Name = p.Region.Name,    
                Description = p.Region.Description,
            }
        });

        return await _provinceRepository.PaginateAsync(provinceDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}