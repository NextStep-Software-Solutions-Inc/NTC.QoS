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

        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<GenerationDto>>> GetAllGenerations(int pageSize = 10, int pageNumber = 1)
        {
            var result = await _mediator.Send(new GetAllGenerationsRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });

            return Ok(result);
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
            var generation = await _mediator.Send(new CreateGenerationRequest
            {
                Generation = generationDto
            });

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
        public async Task<ActionResult<ApiResponse<Domain.Aggregates.Generation>>> DeleteGenerationById(Guid id)
        {
            var generation = await _mediator.Send(new DeleteGenerationByIdRequest(id));

            return generation.Succeeded ? Ok(generation) : BadRequest(generation);
        }
    }
}
