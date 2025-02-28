using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("speedtests", Name = "speedtests")]
public class SpeedtestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpeedtestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllSpeedtests(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllSpeedtestsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        return Ok(result);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> GetSpeedtestById(Guid id)
    {
        Guid speedtestId = id;
        var request = new GetSpeedtestByIdRequest(speedtestId);
        var speedtest = await _mediator.Send(request);
        return speedtest.Succeeded ? Ok(speedtest) : BadRequest(speedtest);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Speedtest>>> DeleteSpeedtestById(Guid id)
    {
        var speedtest = await _mediator.Send(new DeleteSpeedtestByIdRequest(id));

        return speedtest.Succeeded ? Ok(speedtest) : BadRequest(speedtest);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> Post(CreateSpeedtestDto speedtestDto)
    {
        var speedtest = await _mediator.Send(new CreateSpeedtestRequest(speedtestDto));

        return speedtest.Succeeded ? Ok(speedtest) : BadRequest(speedtest);
    }

}
