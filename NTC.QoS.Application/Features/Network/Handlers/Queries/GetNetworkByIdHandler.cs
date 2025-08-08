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
                Id = network.NetworkTemplateId ?? Guid.Empty,
                Name = network.NetworkTemplate?.Name ?? string.Empty,
                Description = network.NetworkTemplate?.Description ?? string.Empty,
                URL = network.NetworkTemplate?.URL ?? string.Empty
            },
            SystemTypeName = network.SystemType?.Name ?? string.Empty,
            UpLink = new()
            {
                Id = network.UpLinkId,
                Type = network.UpLink.Type.ToString(),
                CenterFrequency = network.UpLink.CenterFrequency,
            },
            DownLink = new()
            {
                Id = network.DownLinkId,
                Type = network.DownLink.Type.ToString(),
                CenterFrequency = network.DownLink.CenterFrequency,
            },
            ServiceProvider = new()
            {
                Id = network.ServiceProvider.Id,
                MNC = network.ServiceProvider.MNC,
                MCC = network.ServiceProvider.MCC,
                Provider = new()
                {
                    Id = network.ServiceProvider.ProviderId,
                    Name = network.ServiceProvider.Provider?.Name ?? string.Empty,
                    Description = network.ServiceProvider.Provider?.Description,
                    Color = network.ServiceProvider.Provider?.Color ?? string.Empty
                },
                Service = new()
                {
                    Id = network.ServiceProvider.ServiceId,
                    Name = network.ServiceProvider.Service?.Name ?? string.Empty,
                    Type = network.ServiceProvider.Service?.Type.ToString() ?? string.Empty
                }
            },
            ULCenterFreq = network.ULCenterFreq,
            DLCenterFreq = network.DLCenterFreq,
            Bandwidth = network.Bandwidth
        };

        return new ApiResponse<NetworkDto>
        {
            Data = result,
            Message = "Network retrieved successfully.",
            Succeeded = true
        };
    }
}
