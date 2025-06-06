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
        var allMunicipalities = _municipalityRepository.Query(m => !m.IsDeleted);

        var municipalityDtos = allMunicipalities.Select(m => new MunicipalityDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            IsCity = m.IsCity,
            Province = new ProvinceDto
            {
                Id = m.ProvinceId,
                Name = m.Province != null ? m.Province.Name : string.Empty,
                Description = m.Province != null ? m.Province.Description : null,
                Region = new RegionDto
                {
                    Id = m.Province != null && m.Province.Region != null ? m.Province.Region.Id : Guid.Empty,
                    Name = m.Province != null && m.Province.Region != null ? m.Province.Region.Name : string.Empty,
                    Description = m.Province != null && m.Province.Region != null ? m.Province.Region.Description : null,
                }
            }
        });

        municipalityDtos = municipalityDtos
            .Where(m => !request.ProvinceId.HasValue || (m.Province != null && m.Province.Id == request.ProvinceId));

        return await _municipalityRepository.PaginateAsync(municipalityDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
