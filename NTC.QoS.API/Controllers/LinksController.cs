using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("links", Name = "links")]
[Authorize]

public class LinksController : ControllerBase
{
    private readonly IMediator _mediator;

    public LinksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Post(CreateLinkDto linkDto)
    {
        var link = await _mediator.Send(new CreateLinkRequest
        {
            Link = linkDto
        });
        return link.Succeeded ? Ok(link) : BadRequest(link);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<LinkDto>>> GetAllLinks(
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? bandIds = null,
        int pageSize = 10,
        int pageNumber = 1
    )
    {
        var result = await _mediator.Send(new GetAllLinksRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ServiceProviderIds = serviceProviderIds,
            BandIds = bandIds,
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LinkDto>>> GetLinkById(Guid id)
    {
        var request = new GetLinkByIdRequest(id);
        var link = await _mediator.Send(request);
        return link.Succeeded ? Ok(link) : BadRequest(link);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<LinkDto>>> UpdateLinkById(Guid id, UpdateLinkDto updateLink)
    {
        var link = await _mediator.Send(new UpdateLinkByIdRequest
        {
            Id = id,
            Link = updateLink
        });

        return link.Succeeded ? Ok(link) : BadRequest(link);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLinkById(Guid id)
    {
        var link = await _mediator.Send(new DeleteLinkByIdRequest(id));
        return link.Succeeded ? Ok(link) : BadRequest(link);
    }
}
