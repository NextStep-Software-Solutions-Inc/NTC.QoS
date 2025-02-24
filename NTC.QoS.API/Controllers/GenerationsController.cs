using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.API.Controllers
{
    [ApiController]
    [Route("generations", Name = "generations")]
    public class GenerationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GenerationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("id")]
        public async Task<ActionResult<ApiResponse<GenerationDto>>> GetGenerationById(Guid id)
        {
            Guid generationId = id;
            var request = new GetGenerationByIdRequest(generationId);
            var generations = await _mediator.Send(request);
            return generations.Succeeded ? Ok(generations) : BadRequest(generations);
        }

        [HttpPost]
        public async Task<IActionResult> Post(CreateGenerationDto generationDto)
        {
            var generation = await _mediator.Send(new CreateGenerationRequest(generationDto));

            return generation.Succeeded ? Ok(generation) : BadRequest(generation);
        }
    }
}
