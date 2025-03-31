using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("zones", Name = "zones")]
public class ZonesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ZonesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> Post(CreateZoneDto zoneDto)
    {
        var zone = await _mediator.Send(new CreateZoneRequest
        {
            Zone = zoneDto
        });

        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ZoneDto>>> GetAllZones(int pageSize = 10, int pageNumber = 1, Guid? barangayId = null)
    {
        var request = new GetAllZonesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            BarangayId = barangayId,
        };
        var zones = await _mediator.Send(request);
        return Ok(zones);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> GetZoneById(Guid id)
    {
        var request = new GetZoneByIdRequest(id);
        var zone = await _mediator.Send(request);
        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> UpdateZoneById(Guid id, UpdateZoneDto updateZone)
    {
        var zone = await _mediator.Send(new UpdateZoneByIdRequest(id, new UpdateZoneDto
        {
            Name = updateZone.Name,
        }));
        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteZoneByIdRequest(id);
        var zone = await _mediator.Send(request);

        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }
}
