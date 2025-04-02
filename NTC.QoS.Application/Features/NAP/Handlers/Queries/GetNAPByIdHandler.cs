using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetNAPByIdHandler(INAPRepository NAPRepository) : IRequestHandler<GetNAPByIdRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<ApiResponse<NAPDto>> Handle(GetNAPByIdRequest request, CancellationToken cancellationToken)
    {
        var NAP = await _NAPRepository.GetByIdAsync(request.Id, cancellationToken);

        if (NAP is null)
        {
            return new ApiResponse<NAPDto>
            {
                Succeeded = false,
                Message = "NAP not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["NAP with the specified Id does not exist."]
                    }
                }
            };
        }

        return new ApiResponse<NAPDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = new ()
            {
                Id = NAP.Id,
                File = new ()
                {
                    Id = NAP.FileId ?? Guid.Empty,
                    Name = NAP.File?.Name ?? string.Empty,
                    Path = NAP.File?.Path ?? string.Empty,
                },
                ServiceProvider = new ()
                {
                    Id = NAP.ServiceProviderId,
                    Service = new ()
                    {
                        Id = NAP.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = NAP.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new ()
                    {
                        Id = NAP.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Color = NAP.ServiceProvider?.Provider?.Color ?? string.Empty
                    }
                },
                Zone = new ()
                {
                    Id = NAP.ZoneId ?? Guid.Empty,
                    Name = NAP.Zone?.Name ?? string.Empty,
                    Barangay = new ()
                    {
                        Id = NAP.Zone?.BarangayId ?? Guid.Empty,
                        Name = NAP.Zone?.Barangay?.Name ?? string.Empty,
                        Description = NAP.Zone?.Barangay?.Description,
                        Municipality = new ()
                        {
                            Id = NAP.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                            Name = NAP.Zone?.Barangay?.Municipality?.Name ?? string.Empty,
                            Description = NAP.Zone?.Barangay?.Description,
                            IsCity = NAP.Zone?.Barangay?.Municipality?.IsCity ?? false,
                            Province = new ()
                            {
                                Id = NAP.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                                Name = NAP.Zone?.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                                Description = NAP.Zone?.Barangay?.Municipality?.Province?.Description,
                                Region = new ()
                                {
                                    Id = NAP.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                    Name = NAP.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                    Description = NAP.Zone?.Barangay?.Municipality?.Province?.Region?.Description
                                }
                            }

                        }
                    },
                },
                CoorLatitude = NAP.CoorLatitude ?? string.Empty,
                CoorLongitude = NAP.CoorLongitude ?? string.Empty,
            }
        };
    }
}
