using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("networks", Name= "networks")]
public class NetworksController : ControllerBase
{
    private readonly IMediator _mediator;
    public NetworksController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<NetworkDto>>> GetAllNetworks(string? search, int pageNumber = 1, int pageSize = 10)
    {
        var request = new GetAllNetworksRequest()
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var networks = await _mediator.Send(request);
        return Ok(networks);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NetworkDto>>> GetNetworkById(Guid id)
    {
        Guid Id = id;
        var request = new GetNetworkByIdRequest(Id);
        var network = await _mediator.Send(request);
        return network.Succeeded ? Ok(network) : BadRequest(network);
    }
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<NetworkDto>>> UpdateNetworkById(Guid id, UpdateNetworkDto updateNetwork)
    {
        var network = await _mediator.Send(new UpdateNetworkByIdRequest(id, new UpdateNetworkDto
        {
            Name = updateNetwork.Name,
            SystemType = updateNetwork.SystemType,
            UplinkId = updateNetwork.UplinkId,
            DownLinkId = updateNetwork.DownLinkId
        }));
        return network.Succeeded ? Ok(network) : BadRequest(network);
    }



}
