using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class CreateNetworkHandler(IRepository<Domain.Aggregates.Network> repository) : IRequestHandler<CreateNetworkRequest, ApiResponse<NetworkDto>>
{
    private readonly IRepository<Domain.Aggregates.Network> _repository = repository;
    public async Task<ApiResponse<NetworkDto>> Handle(CreateNetworkRequest request, CancellationToken cancellationToken)
    {
        var network = await _repository.FindAsync(n => n.Name == request.CreateNetwork.Name && !n.IsDeleted, cancellationToken);
        if (network is not null)
        {
            return new ApiResponse<NetworkDto>
            {
                Message = "Network already exist.",
                Succeeded = false,
            };
        }
        var systemType = EnumHelper<NetworkSystemTypeEnum>.GetValue(request.CreateNetwork.SystemType);
        var newNetwork = new Domain.Aggregates.Network
        {
            Name = request.CreateNetwork.Name,
            NetworkTemplateId = request.CreateNetwork.NetworkTemplateId,
            SystemType = systemType,
            UpLinkId = request.CreateNetwork.UplinkId,
            DownLinkId = request.CreateNetwork.DownLinkId
        };
        _repository.Add(newNetwork);
        await _repository.SaveChangesAsync();
        return new ApiResponse<NetworkDto>
        {
            Data = new NetworkDto
            {
                Id = newNetwork.Id,
                Name = newNetwork.Name,
                NetworkTemplateId = newNetwork.NetworkTemplateId,
                NetworkTemplate = newNetwork.NetworkTemplate != null ? new NetworkTemplateDto
                {
                    Name = newNetwork.NetworkTemplate!.Name
                } : null!,
                SystemType = EnumHelper<NetworkSystemTypeEnum>.GetName( newNetwork.SystemType),
                UplinkId = newNetwork.UpLinkId,
                DownLinkId = newNetwork.DownLinkId
            }, 
            Succeeded = true,
            Message = "Network retrieved successfully."
        };

    }

}
