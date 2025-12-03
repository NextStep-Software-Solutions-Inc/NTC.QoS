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
            NetworkTemplate = network.NetworkTemplate == null ? null : new()
            {
                Id = network.NetworkTemplateId ?? Guid.Empty,
                Name = network.NetworkTemplate.Name,
                Description = network.NetworkTemplate.Description,
                URL = network.NetworkTemplate.URL
            },
            SystemTypeName = network.SystemType?.Name ?? string.Empty,
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
            Band = new()
            {
                Id = network.Band.Id,
                Name = network.Band.Name,
                Description = network.Band.Description
            },
            Municipality = network.Municipality == null ? null : new()
            {
                Id = network.Municipality.Id,
                Name = network.Municipality.Name,
                Description = network.Municipality.Description,
                IsCity = network.Municipality.IsCity,
                Code = network.Municipality.Code,
                Area = network.Municipality.Area,
                Province = network.Municipality.Province == null ? null : new()
                {
                    Id = network.Municipality.Province.Id,
                    Name = network.Municipality.Province.Name,
                    Description = network.Municipality.Province.Description,
                    Code = network.Municipality.Province.Code
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
