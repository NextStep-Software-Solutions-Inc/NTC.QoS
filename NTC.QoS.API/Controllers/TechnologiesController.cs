using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("technologies", Name = "technologies")]
public class TechnologiesController: ControllerBase
{
    private readonly IMediator _mediator;

    public TechnologiesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TechnologyDto>>> GetAllTechnologies(int pageSize = 10, int pageNumber = 1, Guid? generationId = null)
    {
        var request = new GetAllTechnologiesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            GenerationId = generationId,
        };
        var technologies = await _mediator.Send(request);
        return Ok(technologies);
    }

    [HttpGet("id")]
    public async Task<ActionResult<ApiResponse<TechnologyDto>>> GetTechnologyById(Guid technologyId)
    {
        var request = new GetTechnologyByIdRequest(technologyId);
        var technology = await _mediator.Send(request);
        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTechnology(CreateTechnologyDto technologyDto)
    {
        var technology = await _mediator.Send(new CreateTechnologyRequest
        {
            Technology = technologyDto
        });

        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }

    [HttpPut("id")]
    public async Task<ActionResult<ApiResponse<UpdateTechnologyDto>>> Put(Guid technologyId, UpdateTechnologyDto updateTechnologyDto)
    {
        var technology = await _mediator.Send(new UpdateTechnologyRequest
        {
            Id = technologyId,
            Technology = updateTechnologyDto
        });

        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }

    [HttpDelete("id")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid technologyId)
    {
        var request = new DeleteTechnologyByIdRequest(technologyId);
        var technology = await _mediator.Send(request);

        return technology.Succeeded ? Ok(technology) : BadRequest(technology);
    }
}
