using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetNAPByIdHandler(INAPRepository NAPRepository) : IRequestHandler<GetNAPByIdRequest, ApiResponse<NAPByIdDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<ApiResponse<NAPByIdDto>> Handle(GetNAPByIdRequest request, CancellationToken cancellationToken)
    {
        var NAP = await _NAPRepository.GetByIdAsync(request.Id, cancellationToken);

        if (NAP is null)
        {
            return new ApiResponse<NAPByIdDto>
            {
                Succeeded = false,
                Message = "NAP not found."
            };
        }

        return new ApiResponse<NAPByIdDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = new NAPByIdDto()
            {
                Id = NAP.Id,
                FileId = NAP.FileId,
                ServiceProvider = new ServiceProviderNAPDto
                {
                    Id = NAP.ServiceProviderId,
                    Service = new ServiceNAPDto
                    {
                        Id = NAP.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Service?.Name ?? "Unknown",
                        Type = NAP.ServiceProvider?.Service?.Type ?? ServiceTypeEnum.Unknown
                    },
                    Provider = new ProviderNAPDto
                    {
                        Id = NAP.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = NAP.ServiceProvider?.Provider?.Name ?? "Unknown",
                    }
                },
                Zone = new ZoneDto
                {
                    Id = NAP.ZoneId ?? Guid.Empty,
                    Name = NAP.Zone?.Name ?? "Unknown",
                    Barangay = new BarangayDto
                    {
                        Id = NAP.Zone?.BarangayId ?? Guid.Empty,
                        Name = NAP.Zone?.Barangay?.Name ?? "Unknown",
                        Description = NAP.Zone?.Barangay?.Description,
                        Municipality = new MunicipalityDto
                        {
                            Id = NAP.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                            Name = NAP.Zone?.Barangay?.Municipality?.Name ?? "Unknown",
                            Description = NAP.Zone?.Barangay?.Description,
                            IsCity = NAP.Zone?.Barangay?.Municipality?.IsCity ?? false,
                            Province = new ProvinceDto
                            {
                                Id = NAP.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                                Name = NAP.Zone?.Barangay?.Municipality?.Province?.Name ?? "Unknown",
                                Description = NAP.Zone?.Barangay?.Municipality?.Province?.Description,
                                Region = new RegionDto
                                {
                                    Id = NAP.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                    Name = NAP.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? "Unknown",
                                    Description = NAP.Zone?.Barangay?.Municipality?.Province?.Region?.Description
                                }
                            }

                        }
                    },
                },
                CoorLatitude = NAP.CoorLatitude ?? "Unknown",
                CoorLongitude = NAP.CoorLongitude ?? "Unknown",
            }
        };
    }
}
