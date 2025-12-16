using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Authorization;
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
    [RequirePermission(Permissions.CreateUserRegions)]
    public async Task<ActionResult<ApiResponse<UserRegionDto>>> CreateUserRegion(CreateUserRegionDto createUserRegion)
    {
        var userRegion = await _mediator.Send(new CreateUserRegionRequest { UserRegion = createUserRegion });
        return userRegion.Succeeded ? Ok(userRegion) : BadRequest(userRegion);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewUserRegions)]
    public async Task<ActionResult<PaginatedResponse<UserRegionDto>>> GetAllUserRegions(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageNumber = 1,
        [FromQuery] List<Guid>? userIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] List<string>? authUserIds = null,
        [FromQuery] List<string>? statuses = null)
    {
        var userRegions = await _mediator.Send(new GetAllUserRegionsRequest
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            UserIds = userIds,
            RegionIds = regionIds,
            AuthUserIds = authUserIds,
            Statuses = statuses
        });
        return Ok(userRegions);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewUserRegions)]
    public async Task<ActionResult<ApiResponse<UserRegionDto>>> GetUserRegionById(Guid id)
    {
        var userRegion = await _mediator.Send(new GetUserRegionByIdRequest(id));
        return userRegion.Succeeded ? Ok(userRegion) : BadRequest(userRegion);
    }

    [HttpPut("{id}/status")]
    [RequirePermission(Permissions.UpdateUserRegions)]
    public async Task<ActionResult<ApiResponse<UserRegionDto>>> UpdateUserRegionStatus(Guid id, UpdateUserRegionStatusDto updateUserRegion)
    {
        var userRegion = await _mediator.Send(new UpdateUserRegionStatusByIdRequest(id, updateUserRegion));
        return userRegion.Succeeded ? Ok(userRegion) : BadRequest(userRegion);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteUserRegions)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUserRegion(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserRegionByIdRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
