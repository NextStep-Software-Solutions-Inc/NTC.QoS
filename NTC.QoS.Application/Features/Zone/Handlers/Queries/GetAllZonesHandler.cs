using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Application.Features.Zone.Handlers.Queries;

public class GetAllZonesHandler(IZoneRepository zoneRepository) : IRequestHandler<GetAllZonesRequest, PaginatedResponse<ZoneDto>>
{
    private readonly IZoneRepository _zoneRepository = zoneRepository;

    public async Task<PaginatedResponse<ZoneDto>> Handle(GetAllZonesRequest request, CancellationToken cancellationToken)
    {
        var allZones = await _zoneRepository.ListAsync(z => !z.IsDeleted, cancellationToken);

        if (request.BarangayId.HasValue)
        {
            allZones = allZones.Where(z => z.BarangayId == request.BarangayId.Value).ToList();
        }

        var zoneDtos = allZones.Select(z => new ZoneDto
        {
            Id = z.Id,
            Name = z.Name,
            BarangayId = z.BarangayId,
            Barangay = z.Barangay != null ? new BarangayDto
            {
                Id = z.Barangay.Id,
                MunicipalityId = z.Barangay.MunicipalityId,
                Name = z.Barangay.Name
            } : null
        }).ToList();

        return new PaginatedResponse<ZoneDto>(zoneDtos, request.PageNumber, request.PageSize);
    }
}
