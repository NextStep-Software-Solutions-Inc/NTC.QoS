using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<PaginatedResponse<RegionDto>>> GetAllRegions(int pageSize = 1, int pageNumber = 10  )
    {
        var request = new GetAllRegionsRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var regions = await _mediator.Send(request);
        return Ok(regions);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RegionDto>>> GetRegionById(Guid id)
    {
        Guid regionId = id;
        var request = new GetRegionByIdRequest(regionId);
        var regions = await _mediator.Send(request);
        return regions.Succeeded ? Ok(regions) : BadRequest(regions);
    }
    [HttpPost]
    public async Task<IActionResult> Post(CreateRegionDto regionDto)
    {
        var region = await _mediator.Send(new CreateRegionRequest
        {
            Region = regionDto
        });

        return region.Succeeded ? Ok(region) : BadRequest(region);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = new DeleteRegionRequest(id);
        var response = await _mediator.Send(request);

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, UpdateRegionDto updateRegionDto)
    {
        var region = await _mediator.Send(new UpdateRegionRequest
        {
            Id = id,
            Region = updateRegionDto
        });

        return region.Succeeded ? Ok(region) : BadRequest(region);
    }

}
