using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("technologies", Name = "technologies")]
[Authorize]

public class TechnologiesController: ControllerBase
{
    private readonly IMediator _mediator;

    public TechnologiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequirePermission(Permissions.CreateTechnologies)]
    public async Task<IActionResult> CreateTechnology(CreateTechnologyDto technologyDto)
    {
        var technology = await _mediator.Send(new CreateTechnologyRequest
        {
            Technology = technologyDto
        });

        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewTechnologies)]
    public async Task<ActionResult<PaginatedResponse<TechnologyDto>>> GetAllTechnologies(int pageSize = 10, int pageNumber = 1, [FromQuery] List<Guid>? generationIds = null)
    {
        var request = new GetAllTechnologiesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            GenerationIds = generationIds,
        };
        var technologies = await _mediator.Send(request);
        return Ok(technologies);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewTechnologies)]
    public async Task<ActionResult<ApiResponse<TechnologyDto>>> GetTechnologyById(Guid id)
    {
        var request = new GetTechnologyByIdRequest(id);
        var technology = await _mediator.Send(request);
        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }

    [HttpPut("{id}")]
    [RequirePermission(Permissions.UpdateTechnologies)]
    public async Task<ActionResult<ApiResponse<UpdateTechnologyDto>>> UpdateTechnologyById(Guid id, UpdateTechnologyDto updateTechnologyDto)
    {
        var technology = await _mediator.Send(new UpdateTechnologyRequest
        {
            Id = id,
            Technology = updateTechnologyDto
        });

        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteTechnologies)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTechnologyById(Guid id)
    {
        var request = new DeleteTechnologyByIdRequest(id);
        var technology = await _mediator.Send(request);

        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }
}
