using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Queries;

public class GetAllMunicipalitiesHandler(IMunicipalityRepository municipalityRepository) : IRequestHandler<GetAllMunicipalitiesRequest, PaginatedResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<PaginatedResponse<MunicipalityDto>> Handle(GetAllMunicipalitiesRequest request, CancellationToken cancellationToken)
    {
        var allMunicipalities = await _municipalityRepository.ListAsync(m => !m.IsDeleted, cancellationToken);

        if (request.ProvinceId.HasValue)
        {
            allMunicipalities = allMunicipalities.Where(m => m.ProvinceId == request.ProvinceId.Value).ToList();
        }

        var municipalityDtos = allMunicipalities.Select(m => new MunicipalityDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            IsCity = m.IsCity,
            Province = new ProvinceDto
            {
                Id = m.ProvinceId,
                Name = m.Province?.Name ?? "Unknown",
                Description = m.Province?.Description,
                Region = new RegionDto
                {
                    Id = m.Province?.Region?.Id ?? Guid.Empty,
                    Name = m.Province?.Region?.Name ?? "Unknown",
                    Description = m.Province?.Region?.Description,
                }
            }
        }).ToList();

        return new PaginatedResponse<MunicipalityDto>(municipalityDtos, request.PageNumber, request.PageSize);
    }
}
