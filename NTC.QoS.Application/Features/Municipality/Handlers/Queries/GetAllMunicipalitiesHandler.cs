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
        var allMunicipalities = await _municipalityRepository.ListAsync(p => !p.IsDeleted, cancellationToken);

        if (request.ProvinceId.HasValue)
        {
            allMunicipalities = allMunicipalities.Where(m => m.ProvinceId == request.ProvinceId.Value).ToList();
        }

        var municipalityDtos = allMunicipalities.Select(p => new MunicipalityDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            ProvinceId = p.ProvinceId,
            IsCity = p.IsCity,
            Province = new ProvinceDto
            {
                Id = p.ProvinceId,
                Name = p.Province?.Name ?? ""
            }
        }).ToList();

        return new PaginatedResponse<MunicipalityDto>(municipalityDtos, request.PageNumber, request.PageSize);
    }
}
