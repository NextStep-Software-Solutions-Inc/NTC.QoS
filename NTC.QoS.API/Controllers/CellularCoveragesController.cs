using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cellular-coverages", Name = "cellular-coverages")]
public class CellularCoveragesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CellularCoverageDto>>> GetAllCellularCoverages([FromQuery] int pageSize = 10,
                                                                                    [FromQuery] int pageNumber = 1,
                                                                                    [FromQuery] List<Guid>? networkIds = null,
                                                                                    double longitude = 0,
                                                                                    double latitude = 0,
                                                                                    double distance = 1500000000)
    {
        var result = await _mediator.Send(new GetAllCellularCoveragesRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            NetworkIds = networkIds,
            Coordinate = new (x: longitude, y: latitude),
            Distance = distance
        });
        return result.Succeeded ? Ok(result) : BadRequest(result);

    }
}
