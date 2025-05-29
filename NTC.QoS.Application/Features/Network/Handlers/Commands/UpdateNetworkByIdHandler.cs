using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class UpdateNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<UpdateNetworkByIdRequest, ApiResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<ApiResponse<NetworkDto>> Handle(UpdateNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var networkQuery = _networkRepository.Query(n => !n.IsDeleted && n.Id == request.Id);
        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Id", [$"Network with ID [{request.Id}] does not exist."] 
                    }
                },
                Message = $"Network not found."
            };
        }

        if (!string.IsNullOrEmpty(request.Network.Name)) network.Name = request.Network.Name;

        var networkExistsQuery = _networkRepository.Query(n => !n.IsDeleted && n.Id != request.Id && n.Name == request.Network.Name);
        var networkExists = await _networkRepository.FirstOrDefaultAsync(networkExistsQuery, cancellationToken);
        if (networkExists is not null)
        {
            return new ApiResponse<NetworkDto>
            {
                Message = "Network already exist.",
                Succeeded = false,
            };
        }

        _networkRepository.Update(network);
        await _networkRepository.SaveChangesAsync(cancellationToken);

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
            SystemType = network.SystemType?.Name ?? string.Empty,
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
            Message = "Network updated successfully.",
            Succeeded = true
        };
    }
}
