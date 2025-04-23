using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Aggregates;
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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> Post(CreateSpeedtestDto speedtestDto)
    {
        var speedtest = await _mediator.Send(new CreateSpeedtestRequest(speedtestDto));

        return speedtest.Succeeded ? Ok(speedtest) : BadRequest(speedtest);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SpeedtestDto>>> GetAllSpeedtests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] List<Guid>? fileIds = null,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? cellIds = null,
        [FromQuery] List<Guid>? generationIds = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<Guid>? serviceIds = null)
    {
        var request = new GetAllSpeedtestsRequest(pageNumber, pageSize, search, status, fileIds, serviceProviderIds, barangayIds, cellIds, generationIds, providerIds, serviceIds);
        var speedtests = await _mediator.Send(request);
        return speedtests.Succeeded ? Ok(speedtests) : BadRequest(speedtests);
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
}
