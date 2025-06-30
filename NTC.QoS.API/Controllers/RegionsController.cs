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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RegionDto>>> CreateRegion([FromForm] CreateRegionDto regionDto)
    {
        var region = await _mediator.Send(new CreateRegionRequest
        {
            Region = regionDto
        });

        return region.Succeeded ? Ok(region) : BadRequest(region);
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

        return regions.Succeeded ? Ok(regions) : BadRequest(regions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RegionDto>>> GetRegionById(Guid id)
    {
        var request = new GetRegionByIdRequest(id);
        var region = await _mediator.Send(request);

        return region.Succeeded ? Ok(region) : BadRequest(region);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateRegionDto>>> UpdateRegionById(Guid id, [FromForm] UpdateRegionDto updateRegionDto)
    {
        var region = await _mediator.Send(new UpdateRegionRequest{ Id = id, Region = updateRegionDto });
        return region.Succeeded ? Ok(region) : BadRequest(region);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteRegionRequest(id);
        var region = await _mediator.Send(request);

        return region.Succeeded ? Ok(region) : BadRequest(region);
    }
}
