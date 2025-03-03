using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Link.Requests;
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
}
