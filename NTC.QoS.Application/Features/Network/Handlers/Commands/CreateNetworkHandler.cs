using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class CreateNetworkHandler(IRepository<Domain.Aggregates.Network> networkRepository, ILinkRepository linkRepository) : IRequestHandler<CreateNetworkRequest, ApiResponse<CreateNetworkDto>>
{
    private readonly IRepository<Domain.Aggregates.Network> _networkRepository = networkRepository;
    private readonly ILinkRepository _linkRepository = linkRepository;
    public async Task<ApiResponse<CreateNetworkDto>> Handle(CreateNetworkRequest request, CancellationToken cancellationToken)
    {
        var systemType = EnumHelper<NetworkSystemTypeEnum>.ParseUserInput(request.Network.SystemType);

        var network = await _networkRepository.FindAsync(n => n.Name == request.Network.Name && n.SystemType == systemType && !n.IsDeleted, cancellationToken);
        if (network is not null)
        {
            return new ApiResponse<CreateNetworkDto>
            {
                Message = "Network already exist.",
                Succeeded = false,
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
            return new ApiResponse<CreateNetworkDto>
            {
                Message = string.Join(" ", errorMessages),
                Succeeded = false,
            };
        }

        var newNetwork = new Domain.Aggregates.Network
        {
            Name = request.Network.Name,
            NetworkTemplateId = request.Network.NetworkTemplateId,
            SystemType = systemType,
            UpLinkId = request.Network.UplinkId,
            DownLinkId = request.Network.DownLinkId
        };
        _networkRepository.Add(newNetwork);
        await _networkRepository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<CreateNetworkDto>
        {
            Data = new CreateNetworkDto
            {
                Name = newNetwork.Name,
                NetworkTemplateId = newNetwork.NetworkTemplateId,
                SystemType = EnumHelper<NetworkSystemTypeEnum>.GetName(newNetwork.SystemType),
                UplinkId = newNetwork.UpLinkId,
                DownLinkId = newNetwork.DownLinkId
            },
            Succeeded = true,
            Message = "Network created successfully."
        };
    }
}
