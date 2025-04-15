using MediatR;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Queries;

public class GetAllBarangaysHandler(
    IBarangayRepository barangayRepository,
    IMunicipalityRepository municipalityRepository,
    IProvinceRepository provinceRepository)
    : IRequestHandler<GetAllBarangaysRequest, PaginatedResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;
    private readonly IProvinceRepository _provinceRepository = provinceRepository;

    public async Task<PaginatedResponse<BarangayDto>> Handle(GetAllBarangaysRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Municipality))
        {
            var municipality = await _municipalityRepository.FindAsync(
                m => m.Name.ToLower() == request.Municipality.Trim().ToLower(),
                cancellationToken
            );

            if (municipality is null)
            {
                return new PaginatedResponse<BarangayDto>([], request.PageNumber, request.PageSize)
                {
                    Message = $"No Barangay found matching the provided Municipality '{request.Municipality}' not found.",
                    Succeeded = false
                };
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Province))
        {
            var province = await _provinceRepository.FindAsync(
                p => p.Name.ToLower() == request.Province.Trim().ToLower(),
                cancellationToken
            );

            if (province is null)
            {
                return new PaginatedResponse<BarangayDto>([], request.PageNumber, request.PageSize)
                {
                    Message = $"No Barangay found matching the provided Province '{request.Province}' not found.",
                    Succeeded = false
                };
            }
        }

        var allBarangays = await _barangayRepository.ListAsync(b => !b.IsDeleted, cancellationToken);

        var barangayDtos = allBarangays.Select(b => new BarangayDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            Municipality = new MunicipalityDto
            {
                Id = b.MunicipalityId,
                Name = b.Municipality?.Name ?? "Unknown",
                IsCity = b.Municipality?.IsCity ?? false,
                Description = b.Municipality?.Description,
                Province = new ProvinceDto
                {
                    Id = b.Municipality?.ProvinceId ?? Guid.Empty,
                    Name = b.Municipality?.Province?.Name ?? "Unknown",
                    Description = b.Municipality?.Province?.Description,
                    Region = new RegionDto
                    {
                        Id = b.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                        Name = b.Municipality?.Province?.Region?.Name ?? "Unknown",
                        Description = b.Municipality?.Province?.Region?.Description,
                    }
                }
            }
        }).ToList();

        barangayDtos = [.. barangayDtos
            .Where(dto =>
                (string.IsNullOrWhiteSpace(request.Name) || dto.Name?.ToLower().Contains(request.Name.ToLower()) == true) &&
                (string.IsNullOrWhiteSpace(request.Municipality) || dto.Municipality?.Name?.ToLower().Contains(request.Municipality.ToLower()) == true) &&
                (string.IsNullOrWhiteSpace(request.Province) || dto.Municipality?.Province?.Name?.ToLower().Contains(request.Province.ToLower()) == true) &&
                (!request.MunicipalityId.HasValue || dto.Municipality?.Id == request.MunicipalityId)
            )];

        if (barangayDtos.Count is 0)
        {
            return new PaginatedResponse<BarangayDto>([], request.PageNumber, request.PageSize)
            {
                Message = "No records found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullBarangays", new[] { "No barangays found." }
                    }
                }
            };
        }
        return new PaginatedResponse<BarangayDto>(barangayDtos, request.PageNumber, request.PageSize)
        {
            Data = barangayDtos,
            Message = "Barangay retrieved successfully.",
            Succeeded = true
        };
    }
}
