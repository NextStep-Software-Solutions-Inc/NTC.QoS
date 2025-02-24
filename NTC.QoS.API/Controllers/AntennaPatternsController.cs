using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

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
    }
}
