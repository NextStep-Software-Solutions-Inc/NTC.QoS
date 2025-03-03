using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("links", Name = "links")]
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

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LinkDto>>> GetLinkById(Guid id)
    {
        var request = new GetLinkByIdRequest(id);
        var link = await _mediator.Send(request);
        return link.Succeeded ? Ok(link) : BadRequest(link);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateLinkDto>>> UpdateLinkById(Guid id, UpdateLinkDto updateLink)
    {
        var link = await _mediator.Send(new UpdateLinkByIdRequest
        {
            Id = id,
            Link = updateLink
        });

        return link.Succeeded ? Ok(link) : BadRequest(link);
    }
}
