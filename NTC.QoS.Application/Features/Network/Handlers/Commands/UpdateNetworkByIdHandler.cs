using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class UpdateNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<UpdateNetworkByIdRequest, ApiResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<ApiResponse<NetworkDto>> Handle(UpdateNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var network = await _networkRepository.GetByIdAsync(request.Id, cancellationToken);
        if (network == null)
        {
            return new ApiResponse<NetworkDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { $"Network with ID [{request.Id}] not found." },
                Message = $"Network with ID [{request.Id}] does not exist."
            };
        }
        var networkType = EnumHelper<NetworkSystemTypeEnum>.GetValue(request.Network.SystemType);
        network.Name = request.Network.Name;
        network.Type = networkType;
        network.UpLinkId = request.Network.UplinkId;
        network.DownLinkId = request.Network.DownLinkId;
        network.NetworkTemplateId = request.Network.NetworkTemplateId;

        _networkRepository.Update(network);
        await _networkRepository.SaveChangesAsync(cancellationToken);


        var result = new NetworkDto
        {
            Id = network.Id,
            Name = network.Name,
            NetworkTemplateId = network.NetworkTemplateId,
            NetworkTemplate = network.NetworkTemplate != null ? new NetworkTemplateDto
            {
                Name = network.NetworkTemplate.Name
            } : null!,

            SystemType = EnumHelper<NetworkSystemTypeEnum>.GetName(network.Type),

            UplinkId = network.UpLinkId,
            Uplink = network.UpLink != null && network.UpLink.Band != null ? new LinkDto
            {
                Name = network.UpLink.Band.Name
            } : null!,

            DownLinkId = network.DownLinkId,
            DownLink = network.DownLink != null && network.DownLink.Band != null ? new LinkDto
            {
                Name = network.DownLink.Band.Name
            } : null!
        };
        return new ApiResponse<NetworkDto>
        {
            Data = result,
            Message = "Network updated successfully.",
            Succeeded = true

        };
    }
}
