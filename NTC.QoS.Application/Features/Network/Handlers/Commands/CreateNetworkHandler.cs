using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class CreateNetworkHandler(
    IRepository<Domain.Aggregates.Network> networkRepository,
    IRepository<Domain.Entities.NetworkTemplate> networkTemplateRepository,
    ILinkRepository linkRepository,
    IRepository<Domain.Entities.SystemType> systemTypeRepository) : IRequestHandler<CreateNetworkRequest, ApiResponse<NetworkDto>>
{
    private readonly IRepository<Domain.Aggregates.Network> _networkRepository = networkRepository;
    private readonly IRepository<Domain.Entities.NetworkTemplate> _networkTemplateRepository = networkTemplateRepository;
    private readonly ILinkRepository _linkRepository = linkRepository;
    private readonly IRepository<Domain.Entities.SystemType> _systemTypeRepository = systemTypeRepository;

    public async Task<ApiResponse<NetworkDto>> Handle(CreateNetworkRequest request, CancellationToken cancellationToken)
    {
        var networkQuery = _networkRepository.Query(n => n.Name.Equals(request.Network.Name) && n.SystemTypeId == request.Network.SystemTypeId);
        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);
        if (network is not null)
        {
            return new ApiResponse<NetworkDto>
            {
                Message = "Network with the same name and system type already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Network already exists."]
                    }
                },
                Succeeded = false,
            };
        }

        var networkTempQuery = _networkTemplateRepository.Query(nt => nt.Id == request.Network.NetworkTemplateId);
        var networkTemplate = await _networkTemplateRepository.FirstOrDefaultAsync(networkTempQuery, cancellationToken);
        if (networkTemplate is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullNetworkTemplate", ["Network Template not found."]
                    }
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
                    {
                        "NullSystemType", ["System Type not found."]
                    }
                },
                Message = "System Type not found."
            };
        }

        var errorMessages = new List<string>();
        bool uplinkValid = true;
        bool downlinkValid = true;

        if (request.Network.UplinkId != Guid.Empty)
        {
            uplinkValid = await _linkRepository.ValidateLinkTypesAsync(request.Network.UplinkId, LinkTypeEnum.Uplink, cancellationToken);
            if (!uplinkValid)
            {
                errorMessages.Add("Invalid Uplink Id.");
            }
        }

        if (request.Network.DownLinkId != Guid.Empty)
        {
            downlinkValid = await _linkRepository.ValidateLinkTypesAsync(request.Network.DownLinkId, LinkTypeEnum.Downlink, cancellationToken);
            if (!downlinkValid)
            {
                errorMessages.Add("Invalid Downlink Id.");
            }
        }

        if (!uplinkValid || !downlinkValid)
        {
            return new ApiResponse<NetworkDto>
            {
                Message = string.Join(" ", errorMessages),
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Invalid", errorMessages.ToArray()
                    }
                },
                Succeeded = false,
            };
        }

        var newNetwork = new Domain.Aggregates.Network
        {
            Name = request.Network.Name,
            NetworkTemplateId = networkTemplate.Id,
            SystemTypeId = systemType.Id,
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
                    Name = newNetwork.NetworkTemplate?.Name ?? string.Empty,
                    Description = newNetwork.NetworkTemplate?.Description ?? string.Empty,
                    URL = newNetwork.NetworkTemplate?.URL ?? string.Empty
                },
                SystemTypeName = newNetwork.SystemType?.Name ?? string.Empty,
                UpLink = new()
                {
                    Id = newNetwork.UpLinkId,
                    Type = newNetwork.UpLink?.Type is not null ? newNetwork.UpLink.Type.ToString() : string.Empty,
                    CenterFrequency = newNetwork.UpLink?.CenterFrequency ?? 0,
                },
                DownLink = new()
                {
                    Id = newNetwork.DownLinkId,
                    Type = newNetwork.DownLink?.Type is not null ? newNetwork.DownLink.Type.ToString() : string.Empty,
                    CenterFrequency = newNetwork.DownLink?.CenterFrequency ?? 0,
                }
            },
            Succeeded = true,
            Message = "Network created successfully."
        };
    }
}
