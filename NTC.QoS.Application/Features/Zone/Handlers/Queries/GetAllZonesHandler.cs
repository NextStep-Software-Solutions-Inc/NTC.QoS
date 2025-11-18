using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Handlers.Queries;

public class GetAllZonesHandler(IZoneRepository zoneRepository) : IRequestHandler<GetAllZonesRequest, PaginatedResponse<ZoneDto>>
{
    private readonly IZoneRepository _zoneRepository = zoneRepository;

    public async Task<PaginatedResponse<ZoneDto>> Handle(GetAllZonesRequest request, CancellationToken cancellationToken)
    {
        var allZonesQuery = _zoneRepository.Query();

        if (request.BarangayId.HasValue)
        {
            allZonesQuery = allZonesQuery.Where(z => z.BarangayId == request.BarangayId.Value);
        }

        var zoneDtosQuery = allZonesQuery
            .OrderBy(z => z.Name)
            .Select(z => new ZoneDto
            {
                Id = z.Id,
                Name = z.Name,
                Barangay = new()
                {
                    Id = z.Barangay.Id,
                    Name = z.Barangay.Name,
                    Description = z.Barangay.Description,
                    Municipality = new()
                    {
                        Id = z.Barangay.MunicipalityId,
                        Name = z.Barangay.Municipality.Name,
                        IsCity = z.Barangay.Municipality.IsCity,
                        Description = z.Barangay.Municipality.Description,
                        Province = new()
                        {
                            Id = z.Barangay.Municipality.Province.Id,
                            Name = z.Barangay.Municipality.Province.Name,
                            Description = z.Barangay.Municipality.Province.Description,
                            Region = new()
                            {
                                Id = z.Barangay.Municipality.Province.Region.Id,
                                Name = z.Barangay.Municipality.Province.Region.Name,
                                Description = z.Barangay.Municipality.Province.Region.Description,
                            }
                        }
                    },
                    Code = z.Barangay.Code ?? string.Empty,
                    Area = z.Barangay.Area
                }
            });

        return await _zoneRepository.PaginateAsync(zoneDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
