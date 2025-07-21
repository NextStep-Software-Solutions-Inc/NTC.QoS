using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.NetworkTemplate.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("network-templates", Name = "network-templates")]
[Authorize]

public class NetworkTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public NetworkTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NetworkTemplateDto>>> CreateNetworkTemplate(CreateNetworkTemplateDto createNetworkTemplate)
    {
        var networkTemplate = await _mediator.Send(new CreateNetworkTemplateRequest(createNetworkTemplate));
        return networkTemplate.Succeeded ? Ok(networkTemplate) : BadRequest(networkTemplate);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<NetworkTemplateDto>>> GetAllNetworkTemplates(string? search, int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllNetworkTemplatesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize, 
            Search = search
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NetworkTemplateDto>>> GetNetworkTemplateById(Guid id)
    {
        var request = new GetNetworkTemplateByIdRequest(id);
        var networkTemplate = await _mediator.Send(request);
        return networkTemplate.Succeeded ? Ok(networkTemplate) : BadRequest(networkTemplate);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<NetworkTemplateDto>>> UpdateNetworkTemplateById(Guid id, UpdateNetworkTemplateDto updateNetworkTemplate)
    {
        var networkTemplate = await _mediator.Send(new UpdateNetworkTemplateRequest(id, new UpdateNetworkTemplateDto
        {
            Name = updateNetworkTemplate.Name,
            Description = updateNetworkTemplate.Description,
            URL = updateNetworkTemplate.URL
        }));
        return networkTemplate.Succeeded ? Ok(networkTemplate) : BadRequest(networkTemplate);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNetworkTemplateById(Guid id)
    {
        var networkTemplate = await _mediator.Send(new DeleteNetworkTemplateByIdRequest(id));

        return networkTemplate.Succeeded ? Ok(networkTemplate) : BadRequest(networkTemplate);
    }
}
