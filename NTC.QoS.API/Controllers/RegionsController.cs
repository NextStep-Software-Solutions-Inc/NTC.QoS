using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.API.Controllers;


[ApiController]
[Route("regions", Name = "regions")]
public class RegionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RegionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RegionDto>>> GetAllRegions(int pageSize = 10, int pageNumber = 1)
    {
        var request = new GetAllRegionsRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var regions = await _mediator.Send(request);
        return Ok(regions);
    }
    [HttpGet("id")]
    public async Task<ActionResult<ApiResponse<RegionDto>>> GetRegionById(Guid regionId)
    {
        var request = new GetRegionByIdRequest(regionId);
        var region = await _mediator.Send(request);
        return region.Succeeded ? Ok(region) : BadRequest(region);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateRegionDto>>> Post(CreateRegionDto regionDto)
    {
        var region = await _mediator.Send(new CreateRegionRequest
        {
            Region = regionDto
        });

        return region.Succeeded ? Ok(region) : BadRequest(region);
    }
    [HttpDelete("id")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid regionId)
    {
        var request = new DeleteRegionRequest(regionId);
        var region = await _mediator.Send(request);

        return region.Succeeded ? Ok(region) : BadRequest(region);
    }

    [HttpPut("id")]
    public async Task<ActionResult<ApiResponse<UpdateRegionDto>>> Put(Guid regionId, UpdateRegionDto updateRegionDto)
    {
        var region = await _mediator.Send(new UpdateRegionRequest
        {
            Id = regionId,
            Region = updateRegionDto
        });

        return region.Succeeded ? Ok(region) : BadRequest(region);
    }
}
