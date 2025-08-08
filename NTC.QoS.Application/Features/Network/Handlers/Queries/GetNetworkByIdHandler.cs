using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Queries;

public class GetNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<GetNetworkByIdRequest, ApiResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;

    public async Task<ApiResponse<NetworkDto>> Handle(GetNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var networkQuery = _networkRepository.Query(n => n.Id == request.Id);
        var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);

        if (network is null)
        {
            return new ApiResponse<NetworkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Id", ["Network not found."] 
                    },
                },
                Message = $"Network with ID [{request.Id}] does not exists."
            };
        }

        var result = new NetworkDto
        {
            Id = network.Id,
            Name = network.Name,
            NetworkTemplate = new()
            {
                Id = network.NetworkTemplate.Id,
                Name = network.NetworkTemplate?.Name ?? string.Empty,
                Description = network.NetworkTemplate?.Description ?? string.Empty,
                URL = network.NetworkTemplate?.URL ?? string.Empty
            },
            SystemTypeName = network.SystemType?.Name ?? string.Empty,
            UpLink = new()
            {
                Id = network.UpLinkId,
                Type = network.UpLink?.Type.ToString() ?? string.Empty,
                CenterFrequency = network.UpLink?.CenterFrequency ?? 0,
            },
            DownLink = new()
            {
                Id = network.DownLinkId,
                Type = network.DownLink?.Type.ToString() ?? string.Empty,
                CenterFrequency = network.DownLink?.CenterFrequency ?? 0,
            },
            ServiceProvider = new()
            {
                Id = network.ServiceProvider?.Id ?? Guid.Empty,
                MNC = network.ServiceProvider?.MNC ?? string.Empty,
                MCC = network.ServiceProvider?.MCC ?? string.Empty,
                Provider = new()
                {
                    Id = network.ServiceProvider?.ProviderId ?? Guid.Empty,
                    Name = network.ServiceProvider?.Provider?.Name ?? string.Empty,
                    Description = network.ServiceProvider?.Provider?.Description,
                    Color = network.ServiceProvider?.Provider?.Color ?? string.Empty
                },
                Service = new()
                {
                    Id = network.ServiceProvider?.ServiceId ?? Guid.Empty,
                    Name = network.ServiceProvider?.Service?.Name ?? string.Empty,
                    Type = network.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                }
            },
            ULCenterFreq = network.ULCenterFreq,
            DLCenterFreq = network.DLCenterFreq,
            Bandwidth = network.Bandwidth,
        };

        return new ApiResponse<NetworkDto>
        {
            Data = result,
            Message = "Network retrieved successfully.",
            Succeeded = true
        };
    }
}
