using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Commands;

public class CreateBarangayHandler(IBarangayRepository repository, IMunicipalityRepository municipalityRepository) : IRequestHandler<CreateBarangayRequest, ApiResponse<BarangayDto>>
{
    private readonly IBarangayRepository _repository = repository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<ApiResponse<BarangayDto>> Handle(CreateBarangayRequest request, CancellationToken cancellationToken)
    {
        var barangayQuery = _repository.Query(b => b.Name == request.Barangay.Name && b.MunicipalityId == request.Barangay.MunicipalityId && !b.IsDeleted);
        var barangay = await _repository.FirstOrDefaultAsync(barangayQuery, cancellationToken);

        if (barangay is not null)
        {
            return new ApiResponse<BarangayDto>
            {
                Message = "Barangay already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["A barangay with the specified name already exists."]
                    }
                }
            };
        }
        var municipalityQuery = _municipalityRepository.Query(m => m.Id == request.Barangay.MunicipalityId && !m.IsDeleted);
        var municipality = await _municipalityRepository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<BarangayDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullMunicipality", ["Municipality not found."]
                    }
                },
                Message = "Municipality not found."
            };
        }
        var newBarangay = new Domain.Entities.Barangay
        {
            MunicipalityId = request.Barangay.MunicipalityId,
            Name = request.Barangay.Name,
            Description = request.Barangay.Description,
        };

        _repository.Add(newBarangay);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<BarangayDto>
        {
            Data = new BarangayDto
            {
                Id = newBarangay.Id,
                Name = newBarangay.Name,
                Description = newBarangay.Description,
                Municipality = new MunicipalityDto
                {
                    Id = newBarangay.MunicipalityId,
                    Name = newBarangay.Municipality?.Name ?? string.Empty,
                    Description = newBarangay.Municipality?.Description,
                    IsCity = newBarangay.Municipality != null && newBarangay.Municipality.IsCity,
                    Province = new ProvinceDto
                    {
                        Id = newBarangay.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = newBarangay.Municipality?.Province?.Name ?? string.Empty,
                        Description = newBarangay.Municipality?.Province?.Description,
                        Region = new RegionDto
                        {
                            Id = newBarangay.Municipality?.Province?.RegionId ?? Guid.Empty,
                            Name = newBarangay.Municipality?.Province?.Region?.Name ?? string.Empty,
                            Description = newBarangay.Municipality?.Province?.Region?.Description
                        }
                    }
                },
            },
            Message = "Barangay created successfully.",
            Succeeded = true
        };
    }
}