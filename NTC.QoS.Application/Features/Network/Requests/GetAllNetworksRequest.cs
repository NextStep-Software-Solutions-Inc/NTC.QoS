using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class GetAllNetworksRequest(int pageNumber, int pageSize, string? search, Guid? networkTemplateId) : IRequest<PaginatedResponse<GetNetworkDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public string? Search { get; set; } = search;
    public Guid? NetworkTemplateId { get; set; } = networkTemplateId;
}
