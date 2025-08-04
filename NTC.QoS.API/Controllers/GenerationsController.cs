using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("generations", Name = "generations")]
[Authorize]

public class GenerationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GenerationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGeneration(CreateGenerationDto generationDto)
    {
        var generation = await _mediator.Send(new CreateGenerationRequest
        {
            Generation = generationDto
        });

        return generation.Succeeded ? Ok(generation) : BadRequest(generation);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GenerationDto>>> GetAllGenerations(int pageSize = 10,
                                                                                        int pageNumber = 1,
                                                                                        [FromQuery] List<Guid>? providerIds = default,
                                                                                        [FromQuery] List<Guid>? serviceIds = default,
                                                                                        [FromQuery] List<Guid>? serviceProviderIds = default)
    {
        var generations = await _mediator.Send(new GetAllGenerationsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            ServiceProviderIds = serviceProviderIds
        });

        return generations.Succeeded ? Ok(generations) : BadRequest(generations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GenerationDto>>> GetGenerationById(Guid id)
    {
        var request = new GetGenerationByIdRequest(id);
        var generation = await _mediator.Send(request);
        return generation.Succeeded ? Ok(generation) : BadRequest(generation);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateGenerationDto>>> UpdateGenerationById(Guid id, UpdateGenerationDto updateGeneration)
    {
        var generation = await _mediator.Send(new UpdateGenerationByIdRequest
        {
            Id = id,
            Generation = updateGeneration
        });

        return generation.Succeeded ? Ok(generation) : BadRequest(generation);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteGenerationById(Guid id)
    {
        var generation = await _mediator.Send(new DeleteGenerationByIdRequest(id));

        return generation.Succeeded ? Ok(generation) : BadRequest(generation);
    }
}
