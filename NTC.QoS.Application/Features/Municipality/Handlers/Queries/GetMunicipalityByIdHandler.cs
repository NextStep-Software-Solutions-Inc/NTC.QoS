using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Queries;

public class GetMunicipalityByIdHandler(IMunicipalityRepository municipalityRepository) : IRequestHandler<GetMunicipalityByIdRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<ApiResponse<MunicipalityDto>> Handle(GetMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var municipality = await _municipalityRepository.GetByIdAsync(request.Id, cancellationToken);

        if (municipality == null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Succeeded = false,
                Message = "Municipality not found."
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
                ProvinceId = municipality.ProvinceId,
                Province = new ProvinceDto
                {
                    Name = municipality.Province?.Name ?? "Unknown"
                }
            }
        };
    }
}
