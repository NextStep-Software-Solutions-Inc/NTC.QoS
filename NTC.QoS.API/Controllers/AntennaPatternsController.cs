using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.API.Controllers
{
    [ApiController]
    [Route("antenna-patterns", Name = "antenna-patterns")]
    public class AntennaPatternsController: ControllerBase
    {
        private readonly IMediator _mediator;

        public AntennaPatternsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<AntennaPatternDto>>> GetAllAntennaPatterns(int pageSize = 10, int pageNumber = 1)
        {
            var result = await _mediator.Send(new GetAllAntennaPatternsRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });

            return Ok(result);
        }

        [HttpGet("id")]
        public async Task<ActionResult<ApiResponse<AntennaPatternDto>>> GetAntennaPatternById(Guid id)
        {
            var request = new GetAntennaPatternByIdRequest(id);
            var antennaPattern = await _mediator.Send(request);
            return antennaPattern.Succeeded ? Ok(antennaPattern) : BadRequest(antennaPattern);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAntennaPattern(CreateAntennaPatternDto antennaPatternDto)
        {
            var antennaPattern = await _mediator.Send(new CreateAntennaPatternRequest
            {
                AntennaPattern = antennaPatternDto
            });

            return antennaPattern.Succeeded ? Ok(antennaPattern) : BadRequest(antennaPattern);
        }
    }
}
