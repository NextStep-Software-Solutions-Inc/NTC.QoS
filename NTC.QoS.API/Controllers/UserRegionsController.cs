using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("user-regions", Name = "user-regions")]
[Authorize]
public class UserRegionsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserRegionDto>>> CreateUserRegion([FromBody] CreateUserRegionDto createUserRegion)
    {
        var request = new CreateUserRegionRequest { UserRegion = createUserRegion };
        var userRegion = await _mediator.Send(request);
        return userRegion.Succeeded ? Ok(userRegion) : BadRequest(userRegion);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserRegionDto>>> GetAllUserRegions(
        int pageSize = 10, 
        int pageNumber = 1,
        [FromQuery] List<Guid>? userIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] List<string>? statuses = null)
    {
        var request = new GetAllUserRegionsRequest 
        { 
            PageSize = pageSize, 
            PageNumber = pageNumber, 
            UserIds = userIds, 
            RegionIds = regionIds, 
            Statuses = statuses 
        };
        var userRegions = await _mediator.Send(request);
        return userRegions.Succeeded ? Ok(userRegions) : BadRequest(userRegions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserRegionDto>>> GetUserRegionById(Guid id)
    {
        var userRegion = await _mediator.Send(new GetUserRegionByIdRequest(id));
        return userRegion.Succeeded ? Ok(userRegion) : BadRequest(userRegion);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserRegionDto>>> UpdateUserRegionById(Guid id, UpdateUserRegionDto updateUserRegion)
    {
        var userRegion = await _mediator.Send(new UpdateUserRegionByIdRequest(id, updateUserRegion));
        return userRegion.Succeeded ? Ok(userRegion) : BadRequest(userRegion);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUserRegionById(Guid id)
    {
        var userRegion = await _mediator.Send(new DeleteUserRegionByIdRequest(id));
        return userRegion.Succeeded ? Ok(userRegion) : BadRequest(userRegion);
    }
}
