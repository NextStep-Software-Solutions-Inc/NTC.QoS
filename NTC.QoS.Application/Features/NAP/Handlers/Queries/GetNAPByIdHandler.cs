using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.DTOs.Zone;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetNAPByIdHandler(INAPRepository NAPRepository) : IRequestHandler<GetNAPByIdRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<ApiResponse<NAPDto>> Handle(GetNAPByIdRequest request, CancellationToken cancellationToken)
    {
        var NAP = await _NAPRepository.GetByIdAsync(request.Id, cancellationToken);

        if (NAP == null)
        {
            return new ApiResponse<NAPDto>
            {
                Succeeded = false,
                Message = "NAP not found."
            };
        }

        return new ApiResponse<NAPDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = new NAPDto()
            {
                Id = NAP.Id,
                ServiceProviderId = NAP.ServiceProviderId,
                ServiceProvider = NAP.ServiceProvider != null
                    ? new ServiceProviderDto
                    {
                        ProviderId = NAP.ServiceProvider.ProviderId,
                        ServiceId = NAP.ServiceProvider.ServiceId,
                        Provider = new ProviderDto
                        {
                            Name = NAP.ServiceProvider.Provider?.Name ?? string.Empty
                        },
                        Service = new ServiceDto
                        {
                            Name = NAP.ServiceProvider.Service?.Name ?? string.Empty
                        }
                    } : null!,
                ZoneId = NAP.ZoneId,
                Zone = NAP.Zone != null
                    ? new ZoneDto
                    {
                        Id = NAP.Zone.Id,
                        Name = NAP.Zone.Name ?? string.Empty
                    } : null!,

                File = NAP.File != null
                    ? new FileDto
                    {
                        Name = NAP.File.Name ?? string.Empty,
                        Path = NAP.File.Path ?? string.Empty
                    }
                    : null!,
                CoorLatitude = NAP.CoorLatitude,
                CoorLongitude = NAP.CoorLongitude,
            }
        };
    }
}
