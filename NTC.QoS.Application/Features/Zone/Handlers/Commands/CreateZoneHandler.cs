using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Handlers.Commands;

public class CreateZoneHandler(IZoneRepository repository, IBarangayRepository barangayRepository) : IRequestHandler<CreateZoneRequest, ApiResponse<ZoneDto>>
{
    private readonly IZoneRepository _repository = repository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<ApiResponse<ZoneDto>> Handle(CreateZoneRequest request, CancellationToken cancellationToken)
    {
        var existingZone = await _repository.FindAsync(z => z.Name == request.Zone.Name && z.BarangayId == request.Zone.BarangayId && !z.IsDeleted, cancellationToken);

        if (existingZone is not null)
        {
            return new ApiResponse<ZoneDto>
            {
                Message = "Zone already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["A zone with the specified name already exists."]
                    }
                }
            };
        }

        var barangay = await _barangayRepository.GetByIdAsync(request.Zone.BarangayId, cancellationToken);
        
        if (barangay is not null)
        {
            return new ApiResponse<ZoneDto>
            {
                Succeeded = true,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullBarangay",
                        ["Barangay does not exist."]
                    }
                },
                Message = "Barangay not found."
            };
        }

        var newZone = new Domain.Entities.Zone
        {
            BarangayId = request.Zone.BarangayId,
            Name = request.Zone.Name,
        };

        _repository.Add(newZone);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ZoneDto>
        {
            Data = new ZoneDto()
            {
                Id = newZone.Id,
                Name = newZone.Name,
                Barangay = new BarangayDto
                {
                    Id = newZone.BarangayId,
                    Name = newZone.Barangay?.Name ?? string.Empty,
                    Description = newZone.Barangay?.Description ?? string.Empty,
                    Municipality = new MunicipalityDto
                    {
                        Id = newZone.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = newZone.Barangay?.Municipality?.Name ?? string.Empty,
                        IsCity = newZone.Barangay?.Municipality?.IsCity ?? false,
                        Description = newZone.Barangay?.Municipality?.Description,
                        Province = new ProvinceDto
                        {
                            Id = newZone.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = newZone.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = newZone.Barangay?.Municipality?.Province?.Description,
                            Region = new RegionDto
                            {
                                Id = newZone.Barangay?.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                                Name = newZone.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = newZone.Barangay?.Municipality?.Province?.Region?.Description,
                            }
                        }
                    }
                }
            },
            Message = "Zone created successfully.",
            Succeeded = true
        };
    }
}