using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class UpdateMunicipalityByIdHandler(IMunicipalityRepository repository) : IRequestHandler<UpdateMunicipalityByIdRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _repository = repository;
    public async Task<ApiResponse<MunicipalityDto>> Handle(UpdateMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var municipality = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", ["Municipality not found."] }
                },
                Message = "The municipality you are trying to edit does not exist."
            };
        }
        municipality.Name = request.Municipality.Name;
        municipality.Description = request.Municipality.Description;
        municipality.IsCity = request.Municipality.IsCity;

        _repository.Update(municipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<MunicipalityDto>
        {
            Succeeded = true,
            Data = new MunicipalityDto
            {
                Id = municipality.Id,
                Name = municipality.Name,
                Description = municipality.Description,
                IsCity = municipality.IsCity,
                Province = new ProvinceDto
                {
                    Id = municipality.ProvinceId,
                    Name = municipality.Province?.Name ?? string.Empty,
                    Description = municipality.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = municipality.Province?.RegionId ?? Guid.Empty,
                        Name = municipality.Province?.Region?.Name ?? string.Empty,
                        Description = municipality.Province?.Region?.Description
                    }
                }
            },
            Message = "Request successful! Your changes have been saved."
        };
    }
}