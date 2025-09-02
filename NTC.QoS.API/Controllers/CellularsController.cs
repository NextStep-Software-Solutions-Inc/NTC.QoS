using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("cellulars", Name = "cellulars")]
[Authorize]

public class CellularsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.CreateCellulars)]
    public async Task<IActionResult> CreateCellular(CreateCellularDto cellularDto)
    {
        var cellular = await _mediator.Send(new CreateCellularRequest
        {
            Cellular = cellularDto
        });
        return cellular.Succeeded ? Ok(cellular) : BadRequest(cellular);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GetCellularDto>>> GetAllCellulars(int pageSize = 10,
                                                                                       int pageNumber = 1,
                                                                                       Guid? cellId = null,
                                                                                       Guid? cellSectorId = null,
                                                                                       Guid? serviceProviderId = null,
                                                                                       Guid? technologyId = null,
                                                                                       Guid? bandId = null)
    {
        var result = await _mediator.Send(new GetAllCellularsRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            CellId = cellId,
            CellSectorId = cellSectorId,
            ServiceProviderId = serviceProviderId,
            TechnologyId = technologyId,
            BandId = bandId
        });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewCellulars)]
    public async Task<ActionResult<ApiResponse<CellularDto>>> GetCellularById(Guid id)
    {
        var request = new GetCellularByIdRequest(id);
        var cellular = await _mediator.Send(request);
        return cellular.Succeeded ? Ok(cellular) : BadRequest(cellular);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteCellulars)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCellularById(Guid id)
    {
        var cellular = await _mediator.Send(new DeleteCellularByIdRequest(id));
        return cellular.Succeeded ? Ok(cellular) : BadRequest(cellular);
    }
}
