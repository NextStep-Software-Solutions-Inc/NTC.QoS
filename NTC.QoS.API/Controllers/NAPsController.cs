using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("NAPs", Name = "NAPs")]
public class NAPsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memoryCache;
    private const string NAPsCacheKey = "naps";

    public NAPsController(IMediator mediator, IMemoryCache memoryCache)
    {
        _mediator = mediator;
        _memoryCache = memoryCache;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NAPDto>>> CreateNAP(CreateNAPDto createNAPDto)
    {
        var NAP = await _mediator.Send(new CreateNAPRequest(createNAPDto));
        if (NAP.Succeeded)
        {
            _memoryCache.Remove(NAPsCacheKey); // Purge cache
            return Ok(NAP);
        }
        return BadRequest(NAP);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)] // tell browser to cache for 5 min
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllNAPs(
        int pageSize = 10,
        int pageNumber = 1,
        Guid? serviceProviderId = null,
        double longitude = 0,
        double latitude = 0,
        double distance = 15000000)
    {
        if (_memoryCache.TryGetValue(NAPsCacheKey, out PaginatedResponse<NAPDto>? cachedNAPs))
            return Ok(cachedNAPs);

        var NAP = await _mediator.Send(new GetAllNAPsRequest(pageSize: pageSize, pageNumber: pageNumber)
        {
            ServiceProviderId = serviceProviderId,
            Coordinate = new(x: longitude, y: latitude),
            Distance = distance
        });

        if (NAP.Succeeded && NAP.Data != null)
            _memoryCache.Set(NAPsCacheKey, NAP, TimeSpan.FromHours(1));

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NAPDto>>> GetNAPById(Guid id)
    {
        var request = new GetNAPByIdRequest(id);
        var NAP = await _mediator.Send(request);
        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<Domain.Aggregates.NAP>>> DeleteNAPsByIds([Required][FromQuery] List<Guid> ids)
    {
        var NAP = await _mediator.Send(new DeleteNAPsByIdsRequest { Ids = ids });
        if (NAP.Succeeded)
        {
            _memoryCache.Remove(NAPsCacheKey); // Purge cache
            return Ok(NAP);
        }
        return BadRequest(NAP);
    }
}
