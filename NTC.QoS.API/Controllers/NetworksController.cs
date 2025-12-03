using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("networks", Name = "networks")]
[Authorize]

public class NetworksController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NetworkDto>>> CreateNetwork([FromBody] CreateNetworkDto createNetwork)
    {
        var network = await _mediator.Send(new CreateNetworkRequest(createNetwork));
        return network.Succeeded ? Ok(network) : BadRequest(network);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<NetworkDto>>> GetAllNetworks(
        int pageNumber = 1, 
        int pageSize = 10, 
        string? search = null, 
        Guid? networkTemplateId = null,
        Guid? technologyId = null,
        Guid? bandId = null,
        Guid? locationId = null,
        string? locationType = null,
        Guid? systemTypeId = null,
        Guid? serviceProviderId = null)
    {
        var request = new GetAllNetworksRequest(
            pageNumber, 
            pageSize, 
            search, 
            networkTemplateId, 
            technologyId, 
            bandId, 
            locationId, 
            locationType, 
            systemTypeId,
            serviceProviderId);
        var networks = await _mediator.Send(request);
        return networks.Succeeded ? Ok(networks) : BadRequest(networks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NetworkDto>>> GetNetworkById(Guid id)
    {
        var request = new GetNetworkByIdRequest(id);
        var network = await _mediator.Send(request);
        return network.Succeeded ? Ok(network) : BadRequest(network);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<NetworkDto>>> UpdateNetworkById(Guid id, UpdateNetworkDto updateNetwork)
    {
        var network = await _mediator.Send(new UpdateNetworkByIdRequest(id, updateNetwork));
        return network.Succeeded ? Ok(network) : BadRequest(network);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNetworkById(Guid id)
    {
        var network = await _mediator.Send(new DeleteNetworkByIdRequest(id));
        return network.Succeeded ? Ok(network) : BadRequest(network);
    }
}
