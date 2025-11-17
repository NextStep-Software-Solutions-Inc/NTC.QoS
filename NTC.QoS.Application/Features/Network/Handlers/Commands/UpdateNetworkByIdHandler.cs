using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Commands;

public class UpdateNetworkByIdHandler(INetworkRepository networkRepository) : IRequestHandler<UpdateNetworkByIdRequest, ApiResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;

    public async Task<ApiResponse<NetworkDto>> Handle(UpdateNetworkByIdRequest request, CancellationToken cancellationToken)
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
                        "Id", [$"Network with ID [{request.Id}] does not exist."]
                    }
                },
                Message = "Network not found."
            };
        }

        network.Name = request.Network.Name ?? network.Name;
        network.ULCenterFreq = request.Network.ULCenterFreq ?? network.ULCenterFreq;
        network.DLCenterFreq = request.Network.DLCenterFreq ?? network.DLCenterFreq;
        network.Bandwidth = request.Network.Bandwidth ?? network.Bandwidth;

        var networkExistsQuery = _networkRepository.Query(n => n.Id != request.Id && n.Name == request.Network.Name);
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
            NetworkTemplate = new()
            {
                Id = network.NetworkTemplate?.Id ?? Guid.Empty,
                Name = network.NetworkTemplate?.Name ?? string.Empty,
                Description = network.NetworkTemplate?.Description ?? string.Empty,
                URL = network.NetworkTemplate?.URL ?? string.Empty
            },
            SystemTypeName = network.SystemType?.Name ?? string.Empty,
            ServiceProvider = new()
            {
                Id = network.ServiceProviderId,
                MNC = network.ServiceProvider?.MNC ?? string.Empty,
                MCC = network.ServiceProvider?.MCC ?? string.Empty,
                Provider = new()
                {
                    Id = network.ServiceProvider?.ProviderId ?? Guid.Empty,
                    Name = network.ServiceProvider?.Provider?.Name ?? string.Empty,
                    Color = network.ServiceProvider?.Provider?.Color ?? string.Empty,
                    Description = network.ServiceProvider?.Provider?.Description ?? string.Empty,
                    LicenseNumber = network.ServiceProvider?.Provider?.LicenseNumber ?? string.Empty
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
            Message = "Network updated successfully.",
            Succeeded = true
        };
    }
}
