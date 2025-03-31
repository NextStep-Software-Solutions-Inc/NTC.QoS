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

        var municipalityDtos = allMunicipalities.Select(m => new MunicipalityDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            IsCity = m.IsCity,
            Province = new ProvinceDto
            {
                Id = m.ProvinceId,
                Name = m.Province?.Name ?? string.Empty,
                Description = m.Province?.Description,
                Region = new RegionDto
                {
                    Id = m.Province?.Region?.Id ?? Guid.Empty,
                    Name = m.Province?.Region?.Name ?? string.Empty,
                    Description = m.Province?.Region?.Description,
                }
            }
        }).ToList();

        municipalityDtos = [..municipalityDtos
            .Where(m => !request.ProvinceId.HasValue || m.Province?.Id == request.ProvinceId)];

        if (municipalityDtos.Count is 0)
        {
            return new PaginatedResponse<MunicipalityDto>(municipalityDtos, request.PageNumber, request.PageSize)
            {
                Data = municipalityDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<MunicipalityDto>(municipalityDtos, request.PageNumber, request.PageSize)
        {
            Data = municipalityDtos,
            Message = "Users retrieved successfully.",
            Succeeded = true
        };
    }
}
