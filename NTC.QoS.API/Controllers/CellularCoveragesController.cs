using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cellular-coverages", Name = "cellular-coverages")]
public class CellularCoveragesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CellularCoverageDto>>> CreateCellularCoverage(CreateCellularCoverageDto cellularCoverage)
    {
        var request = new CreateCellularCoverageRequest
        {
            CellularCoverage = cellularCoverage
        };
        var result = await _mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

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

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CellularCoverageDto>>> GetCellularCoverageById(Guid id)
    {
        var result = await _mediator.Send(new GetCellularCoverageByIdRequest(id));
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    [HttpDelete("/networks/{networkId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellularCoverageByNetworkId(Guid networkId)
    {
        var result = await _mediator.Send(new DeleteCellularCoverageByNetworkIdRequest
        {
            NetworkId = networkId
        });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellularCoveragesByIds([FromQuery] List<Guid> ids)
    {
        var result = await _mediator.Send(new DeleteCellularCoveragesByIdsRequest { Ids = ids });
        return result.Succeeded ? Ok(result) : NotFound(result);
    }
}
