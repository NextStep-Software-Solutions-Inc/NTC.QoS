using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using Microsoft.EntityFrameworkCore;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class CreateNetworkHandler(
    INetworkRepository networkRepository,
    IRepository<Domain.Entities.NetworkTemplate> networkTemplateRepository,
    ILinkRepository linkRepository,
    IRepository<Domain.Entities.SystemType> systemTypeRepository,
    IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateNetworkRequest, ApiResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IRepository<Domain.Entities.NetworkTemplate> _networkTemplateRepository = networkTemplateRepository;
    private readonly ILinkRepository _linkRepository = linkRepository;
    private readonly IRepository<Domain.Entities.SystemType> _systemTypeRepository = systemTypeRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<NetworkDto>> Handle(CreateNetworkRequest request, CancellationToken cancellationToken)
    {
        var networkQuery = _networkRepository.Query(n => n.Name.Equals(request.Network.Name) && n.SystemTypeId == request.Network.SystemTypeId);
        var existingNetwork = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);
        if (existingNetwork is not null)
        {
            return new ApiResponse<NetworkDto>
            {
                Message = "Network with the same name and system type already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["Network already exists."] }
                },
                Succeeded = false,
            };
        }

        var networkTemplateQuery = _networkTemplateRepository.Query(nt => nt.Id == request.Network.NetworkTemplateId);
        var networkTemplate = await _networkTemplateRepository.FirstOrDefaultAsync(networkTemplateQuery, cancellationToken);
        if (networkTemplate is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullNetworkTemplate", ["Network Template not found."] }
                },
                Message = "Network Template not found."
            };
        }

        var systemTypeQuery = _systemTypeRepository.Query(st => st.Id == request.Network.SystemTypeId);
        var systemType = await _systemTypeRepository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);
        if (systemType is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullSystemType", ["System Type not found."] }
                },
                Message = "System Type not found."
            };
        }

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Network.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullServiceProvider", ["Service Provider not found."] }
                },
                Message = "Service Provider not found."
            };
        }

        var errorMessages = new List<string>();

        var uplinkQuery = _linkRepository.Query(l => l.Id == request.Network.UplinkId)
            .Include(l => l.ServiceProvider)
            .Include(l => l.TechnologyBand);
        var uplink = await _linkRepository.FirstOrDefaultAsync(uplinkQuery, cancellationToken);

        if (uplink is null)
        {
            errorMessages.Add("Uplink not found.");
        }
        else
        {
            if (uplink.Type != LinkTypeEnum.Uplink)
            {
                errorMessages.Add("Selected uplink is not of type Uplink.");
            }

            if (uplink.ServiceProviderId != request.Network.ServiceProviderId)
            {
                errorMessages.Add("Uplink service provider must match the network service provider.");
            }
        }

        var downlinkQuery = _linkRepository.Query(l => l.Id == request.Network.DownLinkId)
            .Include(l => l.ServiceProvider)
            .Include(l => l.TechnologyBand);
        var downlink = await _linkRepository.FirstOrDefaultAsync(downlinkQuery, cancellationToken);

        if (downlink is null)
        {
            errorMessages.Add("Downlink not found.");
        }
        else
        {
            if (downlink.Type != LinkTypeEnum.Downlink)
            {
                errorMessages.Add("Selected downlink is not of type Downlink.");
            }

            if (downlink.ServiceProviderId != request.Network.ServiceProviderId)
            {
                errorMessages.Add("Downlink service provider must match the network service provider.");
            }
        }

        if (uplink is not null && downlink is not null)
        {
            if (uplink.TechnologyBandId != downlink.TechnologyBandId)
            {
                errorMessages.Add("Uplink and downlink must have the same technology band.");
            }
        }

        if (errorMessages.Count > 0)
        {
            return new ApiResponse<NetworkDto>
            {
                Message = string.Join(" ", errorMessages),
                Errors = new Dictionary<string, string[]>
                {
                    { "ValidationError", errorMessages.ToArray() }
                },
                Succeeded = false,
            };
        }

        var newNetwork = new Domain.Aggregates.Network
        {
            Name = request.Network.Name,
            NetworkTemplateId = networkTemplate.Id,
            SystemTypeId = systemType.Id,
            ServiceProviderId = serviceProvider.Id,
            UpLinkId = request.Network.UplinkId,
            DownLinkId = request.Network.DownLinkId
        };

        _networkRepository.Add(newNetwork);
        await _networkRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<NetworkDto>
        {
            Data = new NetworkDto
            {
                Id = newNetwork.Id,
                Name = newNetwork.Name,
                NetworkTemplate = new()
                {
                    Id = newNetwork.NetworkTemplateId,
                    Name = networkTemplate.Name,
                    Description = networkTemplate.Description,
                    URL = networkTemplate.URL
                },
                SystemTypeName = systemType.Name,
                ServiceProvider = new()
                {
                    Id = serviceProvider.Id,
                    MNC = serviceProvider.MNC,
                    MCC = serviceProvider.MCC,
                    Provider = new()
                    {
                        Id = serviceProvider.ProviderId,
                        Name = serviceProvider.Provider?.Name ?? string.Empty,
                        Description = serviceProvider.Provider?.Description,
                        Color = serviceProvider.Provider?.Color ?? string.Empty
                    },
                    Service = new()
                    {
                        Id = serviceProvider.ServiceId,
                        Name = serviceProvider.Service?.Name ?? string.Empty,
                        Type = serviceProvider.Service?.Type.ToString() ?? string.Empty
                    }
                },
                UpLink = new()
                {
                    Id = newNetwork.UpLinkId,
                    Type = uplink?.Type.ToString() ?? string.Empty,
                    CenterFrequency = uplink?.CenterFrequency ?? 0,
                },
                DownLink = new()
                {
                    Id = newNetwork.DownLinkId,
                    Type = downlink?.Type.ToString() ?? string.Empty,
                    CenterFrequency = downlink?.CenterFrequency ?? 0,
                }
            },
            Succeeded = true,
            Message = "Network created successfully."
        };
    }
}
