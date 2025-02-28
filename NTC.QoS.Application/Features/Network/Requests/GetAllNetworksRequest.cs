using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class GetAllNetworksRequest : IRequest<PaginatedResponse<NetworkDto>>
{
    public GetAllNetworksRequest( string? search, int pageNumber, int pageSize) 
    { 
        PageNumber = pageNumber;
        PageSize = pageSize;
        Search = search;
    }
    public GetAllNetworksRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public string? Search { get; set; }
}
