using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.GenerationBand.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("generation-bands", Name = "generation-bands")]
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
}
