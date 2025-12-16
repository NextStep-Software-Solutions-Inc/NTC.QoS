using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Handlers.Queries;

public class GetZoneByIdHandler(IZoneRepository zoneRepository) : IRequestHandler<GetZoneByIdRequest, ApiResponse<ZoneDto>>
{
    private readonly IZoneRepository _zoneRepository = zoneRepository;

    public async Task<ApiResponse<ZoneDto>> Handle(GetZoneByIdRequest request, CancellationToken cancellationToken)
    {
        var zoneQuery = _zoneRepository.Query(z => z.Id.Equals(request.Id));
        var zone = await _zoneRepository.FirstOrDefaultAsync(zoneQuery, cancellationToken);

        if (zone is null)
        {
            return new ApiResponse<ZoneDto>
            {
                Succeeded = false,
                Message = "Zone with the specified ID does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullZone", ["Zone not found."]
                    }
                }
            };
        }

        return new ApiResponse<ZoneDto>
        {
            Succeeded = true,
            Message = "Zone retrieved successfully.",
            Data = new ZoneDto()
            {
                Id = zone.Id,
                Name = zone.Name,
                Barangay = new()
                {
                    Id = zone.BarangayId,
                    Name = zone.Barangay?.Name ?? string.Empty,
                    Description = zone.Barangay?.Description ?? string.Empty,
                    Municipality = new()
                    {
                        Id = zone.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = zone.Barangay?.Municipality?.Name ?? string.Empty,
                        IsCity = zone.Barangay?.Municipality?.IsCity ?? false,
                        Description = zone.Barangay?.Municipality?.Description,
                        Province = new()
                        {
                            Id = zone.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = zone.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = zone.Barangay?.Municipality?.Province?.Description,
                            Region = new()
                            {
                                Id = zone.Barangay?.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                                Name = zone.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = zone.Barangay?.Municipality?.Province?.Region?.Description,
                            }
                        }
                    },
                    Code = zone.Barangay?.Code ?? string.Empty,
                    Area = zone.Barangay?.Area ?? 0f
                }
            }
        };
    }
}
