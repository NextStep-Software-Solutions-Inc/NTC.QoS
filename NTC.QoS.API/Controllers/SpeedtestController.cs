using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using System.ComponentModel.DataAnnotations;

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
        string? search = null,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] List<Guid>? cellIds = null,
        [FromQuery] List<Guid>? generationIds = null,
        [FromQuery] List<Guid>? providerIds = null,
        [FromQuery] List<Guid>? serviceIds = null,
        string? signalQuality = null,
        double longitude = 0,
        double latitude = 0,
        double distance = 15000000)
    {
        var speedtTest = await _mediator.Send(new GetAllSpeedtestsRequest(pageNumber, pageSize)
        {
            Search = search,
            ServiceProviderIds = serviceProviderIds,
            CellIds = cellIds,
            GenerationIds = generationIds,
            ProviderIds = providerIds,
            ServiceIds = serviceIds,
            SignalQuality = signalQuality,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance
        });
        return speedtTest.Succeeded ? Ok(speedtTest) : BadRequest(speedtTest);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SpeedtestDto>>> GetSpeedtestById(Guid id)
    {
        Guid speedtestId = id;
        var request = new GetSpeedtestByIdRequest(speedtestId);
        var speedtest = await _mediator.Send(request);
        return speedtest.Succeeded ? Ok(speedtest) : BadRequest(speedtest);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Speedtest>>> DeleteSpeedtestById([Required][FromQuery] List<Guid> ids)
    {
        var speedtests = await _mediator.Send(new DeleteSpeedtestsByIdsRequest { Ids = ids});

        return speedtests.Succeeded ? Ok(speedtests) : BadRequest(speedtests);
    }
}
