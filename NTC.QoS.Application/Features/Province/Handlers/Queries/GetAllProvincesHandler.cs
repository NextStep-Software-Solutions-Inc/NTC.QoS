using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;

public class GetAllProvincesHandler : IRequestHandler<GetAllProvincesRequest, PaginatedResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository;

    public GetAllProvincesHandler(IProvinceRepository provinceRepository)
    {
        _provinceRepository = provinceRepository;
    }

    public async Task<PaginatedResponse<ProvinceDto>> Handle(GetAllProvincesRequest request, CancellationToken cancellationToken)
    {
        var allProvinces = await _provinceRepository.ListAsync(p => !p.IsDeleted, cancellationToken);

        if (request.RegionId.HasValue)
        {
            allProvinces = allProvinces.Where(m => m.RegionId == request.RegionId.Value).ToList();
        }

        var provinceDtos = allProvinces.Select(p => new ProvinceDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Region = new RegionDto
            {
                Id = p.RegionId,
                Name = p.Region?.Name ?? "",
                Description = p.Region?.Description,
            }
        }).ToList();


        return new PaginatedResponse<ProvinceDto>(provinceDtos, request.PageNumber, request.PageSize);
    }
}
