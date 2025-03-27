using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.Network.Handlers.Queries;

public class GetNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<GetNetworkByIdRequest, ApiResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;

    public async Task<ApiResponse<NetworkDto>> Handle(GetNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var network = await _networkRepository.GetByIdAsync(request.Id, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Id", [$"Network with ID [{request.Id}] not found."] 
                    },
                },
                Message = $"Network not found."
            };
        }

        var result = new NetworkDto
        {
            Id = network.Id,
            Name = network.Name,
            NetworkTemplate = new NetworkTemplateDto
            {
                Id = network.NetworkTemplateId,
                Name = network.NetworkTemplate?.Name ?? string.Empty,
                Description = network.NetworkTemplate?.Description ?? string.Empty,
                URL = network.NetworkTemplate?.URL ?? string.Empty
            },
            SystemType = EnumHelper<NetworkSystemTypeEnum>.GetDescription(network.SystemType),
            Uplink = new LinkNetworkDto
            {
                Id = network.UpLinkId,
                Type = network.UpLink?.Type is not null ? EnumHelper<LinkTypeEnum>.GetName(network.UpLink.Type) : string.Empty,
                CenterFrequency = network.UpLink?.CenterFrequency ?? 0,
            },
            DownLink = new LinkNetworkDto
            {
                Id = network.DownLinkId,
                Type = network.DownLink?.Type is not null ? EnumHelper<LinkTypeEnum>.GetName(network.DownLink.Type) : string.Empty,
                CenterFrequency = network.DownLink?.CenterFrequency ?? 0,
            }
        };

        return new ApiResponse<NetworkDto>
        {
            Data = result,
            Message = "Network retrieved successfully.",
            Succeeded = true
        };
    }
}
