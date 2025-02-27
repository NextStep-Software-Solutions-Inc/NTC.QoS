using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.Barangay;

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
                BarangayId = zone.BarangayId,
                Barangay = zone.Barangay != null ? new BarangayDto
                {
                    Id = zone.Barangay.Id,
                    MunicipalityId = zone.Barangay.MunicipalityId,
                    Name = zone.Barangay.Name
                } : null
            }
        };
    }
}
