using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Handlers.Commands;

public class CreateZoneHandler(IZoneRepository repository, IBarangayRepository barangayRepository) : IRequestHandler<CreateZoneRequest, ApiResponse<ZoneDto>>
{
    private readonly IZoneRepository _repository = repository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<ApiResponse<ZoneDto>> Handle(CreateZoneRequest request, CancellationToken cancellationToken)
    {
        var zoneQuery = _repository.Query(z => z.Name == request.Zone.Name && z.BarangayId == request.Zone.BarangayId);
        var zone = await _repository.FirstOrDefaultAsync(zoneQuery, cancellationToken);

        if (zone is not null)
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

        var barangayQuery = _barangayRepository.Query(z => z.Id == request.Zone.BarangayId);
        var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayQuery, cancellationToken);

        if (barangay is null)
        {
            return new ApiResponse<ZoneDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullBarangay", [ "Barangay not found." ]
                    }
                },
                Message = "Barangay not found."
            };
        }

        var newZone = new Domain.Entities.Zone
        {
            BarangayId = barangay.Id,
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
                Barangay = new()
                {
                    Id = newZone.BarangayId,
                    Name = newZone.Barangay?.Name ?? string.Empty,
                    Description = newZone.Barangay?.Description ?? string.Empty,
                    Municipality = new()
                    {
                        Id = newZone.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = newZone.Barangay?.Municipality?.Name ?? string.Empty,
                        IsCity = newZone.Barangay?.Municipality?.IsCity ?? false,
                        Description = newZone.Barangay?.Municipality?.Description,
                        Province = new()
                        {
                            Id = newZone.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = newZone.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = newZone.Barangay?.Municipality?.Province?.Description,
                            Region = new()
                            {
                                Id = newZone.Barangay?.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                                Name = newZone.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = newZone.Barangay?.Municipality?.Province?.Region?.Description,
                            }
                        }
                    },
                    Code = newZone.Barangay?.Code ?? string.Empty,
                    Area = newZone.Barangay?.Area ?? 0f
                }
            },
            Message = "Zone created successfully.",
            Succeeded = true
        };
    }
}
