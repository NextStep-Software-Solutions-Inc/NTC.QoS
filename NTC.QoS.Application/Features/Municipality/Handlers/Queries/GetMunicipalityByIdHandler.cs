using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Queries;

public class GetMunicipalityByIdHandler(IMunicipalityRepository municipalityRepository) : IRequestHandler<GetMunicipalityByIdRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<ApiResponse<MunicipalityDto>> Handle(GetMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var municipality = await _municipalityRepository.GetByIdAsync(request.Id, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Succeeded = false,
                Message = "Municipality with the specified ID does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullMunicipality", ["Municipality not found."]
                    }
                }
            };
        }

        return new ApiResponse<MunicipalityDto>
        {
            Succeeded = true,
            Message = "Municipality retrieved successfully.",
            Data = new MunicipalityDto()
            {
                Id = municipality.Id,
                Name = municipality.Name,
                Description = municipality.Description,
                IsCity = municipality.IsCity,
                Province = new ProvinceDto
                {
                    Id = municipality.ProvinceId,
                    Name = municipality.Province?.Name ?? "Unknown",
                    Description = municipality.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = municipality.Province?.Region?.Id ?? Guid.Empty, 
                        Name = municipality.Province?.Region?.Name ?? "Unknown",
                        Description = municipality.Province?.Region?.Description
                    }
                }
            }
        };
    }
}
