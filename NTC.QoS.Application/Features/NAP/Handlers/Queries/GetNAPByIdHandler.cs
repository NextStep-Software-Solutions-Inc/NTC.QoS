using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Enums;

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
                Zone = new NAPZoneDto
                {
                    Id = NAP.ZoneId,
                    Name = NAP.Zone?.Name ?? "Unknown",
                    Barangay = new BarangayNAPDto
                    {
                        Id = NAP.Zone?.BarangayId ?? Guid.Empty,
                        Name = NAP.Zone?.Barangay?.Name ?? "Unknown"
                    },
                    Municipality = new MunicipalityNAPDto
                    {
                        Id = NAP.Zone?.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = NAP.Zone?.Barangay?.Municipality?.Name ?? "Unknown",
                        IsCity = NAP.Zone?.Barangay?.Municipality?.IsCity ?? false,
                    },
                    Province = new ProvinceNAPDto
                    {
                        Id = NAP.Zone?.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                        Name = NAP.Zone?.Barangay?.Municipality?.Province?.Name ?? "Unknown"
                    },
                    Region = new RegionNAPDto
                    {
                        Id = NAP.Zone?.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                        Name = NAP.Zone?.Barangay?.Municipality?.Province?.Region?.Name ?? "Unknown"
                    }
                },
                CoorLatitude = NAP.CoorLatitude ?? "Unknown",
                CoorLongitude = NAP.CoorLongitude ?? "Unknown",
            }
        };
    }
}
