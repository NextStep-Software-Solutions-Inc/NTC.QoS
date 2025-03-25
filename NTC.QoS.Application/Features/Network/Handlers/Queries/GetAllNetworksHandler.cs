using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.Network.Handlers.Queries;

public class GetAllNetworksHandler(INetworkRepository networkRepository) : IRequestHandler<GetAllNetworksRequest, PaginatedResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<PaginatedResponse<NetworkDto>> Handle(GetAllNetworksRequest request, CancellationToken cancellationToken)
    {
        var allNetworks = await _networkRepository.ListAsync(n => !n.IsDeleted, cancellationToken);
        
        var networkDtos = allNetworks.Select(n => new NetworkDto
        {
            Id = n.Id,
            Name = n.Name,
            NetworkTemplate = new NetworkTemplateDto
            {
                Id = n.NetworkTemplateId,
                Name = n.NetworkTemplate?.Name ?? string.Empty,
                Description = n.NetworkTemplate?.Description ?? string.Empty,
                URL = n.NetworkTemplate?.URL ?? string.Empty
            },
            SystemType = EnumHelper<NetworkSystemTypeEnum>.GetDescription(n.SystemType),
            Uplink = new LinkNetworkDto
            {
                Id = n.UpLinkId,
                Type = n.UpLink?.Type is not null ? EnumHelper<LinkTypeEnum>.GetName(n.UpLink.Type) : string.Empty,
                CenterFrequency = n.UpLink?.CenterFrequency ?? 0,
            },
            DownLink = new LinkNetworkDto
            {
                Id = n.DownLinkId,
                Type = n.DownLink?.Type is not null ? EnumHelper<LinkTypeEnum>.GetName(n.DownLink.Type) : string.Empty,
                CenterFrequency = n.DownLink?.CenterFrequency ?? 0,
            }
        }).ToList();

        networkDtos = [.. networkDtos
            .Where(n => string.IsNullOrEmpty(request.Search) || (n.Name is not null && n.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)))
            .Where(n => !request.NetworkTemplateId.HasValue || n.NetworkTemplate?.Id == request.NetworkTemplateId)];

        if (networkDtos.Count is 0)
        {
            return new PaginatedResponse<NetworkDto>(networkDtos, request.PageNumber, request.PageSize)
            {
                Data = networkDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<NetworkDto>(networkDtos, request.PageNumber, request.PageSize)
        {
            Data = networkDtos,
            Succeeded = true,
            Message = "Networks retrieved successfully."
        };
    }
}
