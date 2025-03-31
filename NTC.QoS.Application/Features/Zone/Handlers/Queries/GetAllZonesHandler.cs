using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

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
            Barangay = new BarangayDto
            {
                Id = z.BarangayId,
                Name = z.Barangay?.Name ?? string.Empty,
                Description = z.Barangay?.Description ?? string.Empty,
                Municipality = new MunicipalityDto
                {
                    Id = z.Barangay?.MunicipalityId ?? Guid.Empty,
                    Name = z.Barangay?.Municipality?.Name ?? string.Empty,
                    IsCity = z.Barangay?.Municipality?.IsCity ?? false,
                    Description = z.Barangay?.Municipality?.Description,
                    Province = new ProvinceDto
                    {
                        Id = z.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = z.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                        Description = z.Barangay?.Municipality?.Province?.Description,
                        Region = new RegionDto
                        {
                            Id = z.Barangay?.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                            Name = z.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                            Description = z.Barangay?.Municipality?.Province?.Region?.Description,
                        }
                    }
                }
            }
        }).ToList();

        return new PaginatedResponse<ZoneDto>(zoneDtos, request.PageNumber, request.PageSize);
    }
}
