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
        var allProvinces = await _provinceRepository.ListAsync(p => !p.IsDeleted, cancellationToken);

        var provinceDtos = allProvinces.Select(p => new ProvinceDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Region = new RegionDto
            {
                Id = p.RegionId,
                Name = p.Region?.Name ?? string.Empty,
                Description = p.Region?.Description,
            }
        }).ToList();

        provinceDtos = [.. provinceDtos.Where(p => !request.RegionId.HasValue || p.Region?.Id == request.RegionId.Value)];

        if (provinceDtos.Count is 0)
        {
            return new PaginatedResponse<ProvinceDto>(provinceDtos, request.PageNumber, request.PageSize)
            {
                Data = provinceDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<ProvinceDto>(provinceDtos, request.PageNumber, request.PageSize)
        {
            Data = provinceDtos,
            Message = "Provinces retrieved successfully.",
            Succeeded = true
        };
    }
}
