using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class CreateMunicipalityHandler(IMunicipalityRepository repository, IProvinceRepository provinceRepository) : IRequestHandler<CreateMunicipalityRequest, ApiResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _repository = repository;
    private readonly IProvinceRepository _provinceRepository = provinceRepository;

    public async Task<ApiResponse<MunicipalityDto>> Handle(CreateMunicipalityRequest request, CancellationToken cancellationToken)
    {
        var municipalityQuery = _repository.Query(m => m.Name == request.Municipality.Name && m.ProvinceId == request.Municipality.ProvinceId && !m.IsDeleted);
        var municipality = await _repository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

        if (municipality is not null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Message = "Municipality already exists.",
                Succeeded = false, 
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["A municipality with the specified name already exists."]
                    }
                }
            };
        }

        var provinceQuery = _provinceRepository.Query(p => p.Id == request.Municipality.ProvinceId && !p.IsDeleted);
        var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

        if (province is null)
        {
            return new ApiResponse<MunicipalityDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvince",["Province not found."]
                    }
                },
                Message = "Province not found."
            };
        }

        var newMunicipality = new Domain.Entities.Municipality
        {
            ProvinceId = province.Id,
            Name = request.Municipality.Name,
            Description = request.Municipality.Description,
            IsCity = request.Municipality.IsCity,
        };

        _repository.Add(newMunicipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<MunicipalityDto>
        {
            Data = new MunicipalityDto
            {
                Id = newMunicipality.Id,
                Name = newMunicipality.Name,
                Description = newMunicipality.Description,
                IsCity = newMunicipality.IsCity,
                Province = new ProvinceDto
                {
                    Id = newMunicipality.ProvinceId,
                    Name = newMunicipality.Province?.Name ?? string.Empty,
                    Description = newMunicipality.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = newMunicipality.Province?.RegionId ?? Guid.Empty,
                        Name = newMunicipality.Province?.Region?.Name ?? string.Empty,
                        Description = newMunicipality.Province?.Region?.Description
                    }
                }
            },
            Message = "Municipality created successfully.",
            Succeeded = true
        };
    }
}
