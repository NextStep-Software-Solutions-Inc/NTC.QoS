using MediatR;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Helper;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Application.Features.Zone.Handlers.Commands;

public class UpdateZoneByIdHandler(IZoneRepository repository) : IRequestHandler<UpdateZoneByIdRequest, ApiResponse<ZoneDto>>
{
    private readonly IZoneRepository _repository = repository;
    public async Task<ApiResponse<ZoneDto>> Handle(UpdateZoneByIdRequest request, CancellationToken cancellationToken)
    {
        var zoneQuery = _repository.Query(z => z.Id == request.Id && !z.IsDeleted);
        var zone = await _repository.FirstOrDefaultAsync(zoneQuery, cancellationToken); 

        if (zone is null)
        {
            return new ApiResponse<ZoneDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullZone", new[] {"Zone not found." }
                    }
                },
                Message = "The zone you are trying to edit does not exist."
            };
        }
        zone.Name = request.Zone.Name;

        _repository.Update(zone);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ZoneDto>
        {
            Succeeded = true,
            Message = "Zone updated successfully.",
            Data = new ZoneDto
            {
                Id = zone.Id,
                Name = zone.Name,
                Barangay = new BarangayDto
                {
                    Id = zone.BarangayId,
                    Name = zone.Barangay?.Name ?? string.Empty,
                    Description = zone.Barangay?.Description ?? string.Empty,
                    Municipality = new MunicipalityDto
                    {
                        Id = zone.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = zone.Barangay?.Municipality?.Name ?? string.Empty,
                        IsCity = zone.Barangay?.Municipality?.IsCity ?? false,
                        Description = zone.Barangay?.Municipality?.Description,
                        Province = new ProvinceDto
                        {
                            Id = zone.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = zone.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = zone.Barangay?.Municipality?.Province?.Description,
                            Region = new RegionDto
                            {
                                Id = zone.Barangay?.Municipality?.Province?.Region?.Id ?? Guid.Empty,
                                Name = zone.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = zone.Barangay?.Municipality?.Province?.Region?.Description,
                            }
                        }
                    },
                    Code = zone.Barangay?.Code ?? string.Empty,
                    Area = zone.Barangay?.Area ?? 0f
                },
            }
        };
    }
}