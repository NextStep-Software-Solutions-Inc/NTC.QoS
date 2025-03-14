using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.Network.Handlers.Queries;

public class GetNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<GetNetworkByIdRequest, ApiResponse<GetNetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<ApiResponse<GetNetworkDto>> Handle(GetNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var network = await _networkRepository.GetByIdAsync(request.Id, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<GetNetworkDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { $"Network with ID [{request.Id}] not found." },
                Message = $"Network with ID [{request.Id}] does not exist."
            };
        }
        var result = new GetNetworkDto
        {
            Id = network.Id,
            Name = network.Name,
            NetworkTemplate = network.NetworkTemplate is not null ? new NetworkTemplateDto
            {
                Id = network.NetworkTemplate.Id,
                Name = network.NetworkTemplate.Name
            } : null,
            SystemType = EnumHelper<NetworkSystemTypeEnum>.GetName(network.SystemType),

            Uplink = network.UpLink is not null ? new LinkNetworkDto
            {
                Id = network.UpLink.Id,
                Type = EnumHelper<LinkTypeEnum>.GetName(network.UpLink.Type),
                CenterFrequency = network.UpLink.CenterFrequency
            } : null!,
            DownLink = network.DownLink is not null ? new LinkNetworkDto
            {
                Id = network.DownLink.Id,
                Type = EnumHelper<LinkTypeEnum>.GetName(network.DownLink.Type),
                CenterFrequency = network.DownLink.CenterFrequency
            } : null!
        };
        return new ApiResponse<GetNetworkDto>
        {
            Data = result,
            Message = "Network retrieved successfully.",
            Succeeded = true
        };
    }
}
