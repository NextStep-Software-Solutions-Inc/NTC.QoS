using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Zone.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("zones", Name = "zones")]
[Authorize]

public class ZonesController : ControllerBase
{
    private readonly IMediator _mediator;
    private const int BrowserCacheDuration = 86400;

    public ZonesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequirePermission(Permissions.CreateZones)]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> Post(CreateZoneDto zoneDto)
    {
        var zone = await _mediator.Send(new CreateZoneRequest
        {
            Zone = zoneDto
        });

        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewZones)]
    [ResponseCache(Duration = BrowserCacheDuration, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new[] { "pageSize", "pageNumber", "barangayId", "search" })]
    public async Task<ActionResult<PaginatedResponse<ZoneDto>>> GetAllZones(int pageSize = 10, int pageNumber = 1, Guid? barangayId = null, [FromQuery] string? search = null)
    {
        var request = new GetAllZonesRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            BarangayId = barangayId,
            Search = search
        };
        var zones = await _mediator.Send(request);
        return Ok(zones);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewZones)]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> GetZoneById(Guid id)
    {
        var request = new GetZoneByIdRequest(id);
        var zone = await _mediator.Send(request);
        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }

    [HttpPut("{id}")]
    [RequirePermission(Permissions.UpdateZones)]
    public async Task<ActionResult<ApiResponse<ZoneDto>>> UpdateZoneById(Guid id, UpdateZoneDto updateZone)
    {
        var zone = await _mediator.Send(new UpdateZoneByIdRequest(id, new UpdateZoneDto
        {
            Name = updateZone.Name,
        }));
        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteZones)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteZoneByIdRequest(id);
        var zone = await _mediator.Send(request);

        return zone.Succeeded ? Ok(zone) : BadRequest(zone);
    }
}
