using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.API.Controllers;

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

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AntennaPatternDto>>> GetAntennaPatternById(Guid id)
    {
        var request = new GetAntennaPatternByIdRequest(id);
        var antennaPattern = await _mediator.Send(request);
        return antennaPattern.Succeeded ? Ok(antennaPattern) : BadRequest(antennaPattern);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AntennaPatternDto>>> CreateAntennaPattern(CreateAntennaPatternDto antennaPatternDto)
    {
        var antennaPattern = await _mediator.Send(new CreateAntennaPatternRequest
        {
            AntennaPattern = antennaPatternDto
        });

        return antennaPattern.Succeeded ? Ok(antennaPattern) : BadRequest(antennaPattern);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<AntennaPatternDto>>> UpdateAntennaPatternById(Guid id, UpdateAntennaPatternDto updateAntennaPattern)
    {
        var antennaPattern = await _mediator.Send(new UpdateAntennaPatternByIdRequest
        {
            Id = id,
            AntennaPattern = updateAntennaPattern
        });

        return antennaPattern.Succeeded ? Ok(antennaPattern) : BadRequest(antennaPattern);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.AntennaPattern>>> DeleteAntennaPatternById(Guid id)
    {
        var antennaPattern = await _mediator.Send(new DeleteAntennaPatternByIdRequest(id));

        return antennaPattern.Succeeded ? Ok(antennaPattern) : BadRequest(antennaPattern);
    }
}
