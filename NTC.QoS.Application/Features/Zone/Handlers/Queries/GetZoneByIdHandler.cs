using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Zone.Handlers.Queries;

public class GetZoneByIdHandler(IZoneRepository zoneRepository) : IRequestHandler<GetZoneByIdRequest, ApiResponse<ZoneDto>>
{
    private readonly IZoneRepository _zoneRepository = zoneRepository;

    public async Task<ApiResponse<ZoneDto>> Handle(GetZoneByIdRequest request, CancellationToken cancellationToken)
    {
        var zone = await _zoneRepository.GetByIdAsync(request.Id, cancellationToken);

        if (zone == null)
        {
            return new ApiResponse<ZoneDto>
            {
                Succeeded = false,
                Message = "Zone not found."
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
                Barangay = new BarangayDto
                {
                    Id = zone.BarangayId,
                    Name = zone.Barangay?.Name ?? "Unknown",
                    Description = zone.Barangay?.Description ?? "Unknown",
                    Municipality = new MunicipalityDto
                    {
                        Id = zone.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name =zone.Barangay?.Municipality?.Name ?? "Unknown",
                        IsCity = zone.Barangay?.Municipality?.IsCity ?? false,
                        Description = zone.Barangay?.Municipality?.Description,
                        Province = new ProvinceDto
                        {
                            Id = zone.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = zone.Barangay?.Municipality?.Province?.Name ?? "Unknown",
                            Description = zone.Barangay?.Municipality?.Province?.Description,
                            Region = new RegionDto
                            {
                                Id = zone.Barangay?.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                                Name = zone.Barangay?.Municipality?.Province?.Region?.Name ?? "Unknown",
                                Description = zone.Barangay?.Municipality?.Province?.Region?.Description,
                            }
                        }
                    }
                }
            }
        };
    }
}
