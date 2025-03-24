using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class UpdateNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<UpdateNetworkByIdRequest, ApiResponse<UpdateNetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<ApiResponse<UpdateNetworkDto>> Handle(UpdateNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var network = await _networkRepository.GetByIdAsync(request.Id, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<UpdateNetworkDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", [$"Network with ID [{request.Id}] does not exist."] }
                },
                Message = $"Network with ID [{request.Id}] does not exist."
            };
        }
        var systemType = EnumHelper<NetworkSystemTypeEnum>.ParseUserInput(request.Network.SystemType);
        network.Name = request.Network.Name;
        network.SystemType = systemType;

        _networkRepository.Update(network);
        await _networkRepository.SaveChangesAsync(cancellationToken);

        var result = new UpdateNetworkDto
        {
            Name = network.Name,
            SystemType = network.SystemType.ToString()
        };
        return new ApiResponse<UpdateNetworkDto>
        {
            Data = result,
            Message = "Network updated successfully.",
            Succeeded = true
        };
    }
}
