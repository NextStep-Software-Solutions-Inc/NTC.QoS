using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("generation-bands", Name = "generation-bands")]
[Authorize]

public class GenerationBandsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GenerationBandsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateGenerationBandDto>>> CreateGenerationBand(CreateGenerationBandDto generationBandDto)
    {
        var generationBand = await _mediator.Send(new CreateGenerationBandRequest
        {
            GenerationBand = generationBandDto
        });
        return generationBand.Succeeded ? Ok(generationBand) : BadRequest(generationBand);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GetGenerationBandDto>>> GetAllGenerationBands(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllGenerationBandsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GetGenerationBandDto>>> GetGenerationBandById(Guid id)
    {
        var request = new GetGenerationBandByIdRequest(id);
        var generationBand = await _mediator.Send(request);
        return generationBand.Succeeded ? Ok(generationBand) : BadRequest(generationBand);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteGenerationBandById(Guid id)
    {
        var request = new DeleteGenerationBandByIdRequest(id);
        var generationBand = await _mediator.Send(request);
        return generationBand.Succeeded ? Ok(generationBand) : BadRequest(generationBand);
    }
}
