using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Generation.Requests;
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

        [HttpPost]
        public async Task<IActionResult> Post(CreateGenerationDto generationDto)
        {
            var generation = await _mediator.Send(new CreateGenerationRequest(generationDto));

            return generation.Succeeded ? Ok(generation) : BadRequest(generation);
        }
    }
}
