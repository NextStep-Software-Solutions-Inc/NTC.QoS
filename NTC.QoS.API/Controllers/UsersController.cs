using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("users", Name = "users")]
[Authorize]

public class UsersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> GetCurrentUser()
    {
        var result = await _mediator.Send(new GetCurrentUserRequest());
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    [RequirePermission(Permissions.CreateUsers)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto userDto)
    {
        var user = await _mediator.Send(new CreateUserRequest(userDto));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpPost("invite")]
    [RequirePermission(Permissions.InviteUsers)]
    public async Task<ActionResult<ApiResponse<UserDto>>> InviteUser(InviteUserDto userDto)
    {
        var user = await _mediator.Send(new InviteUserRequest(userDto));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpGet("invitations")]
    [RequirePermission(Permissions.InviteUsers)]
    public async Task<ActionResult<ApiResponse<List<InvitationDto>>>> GetInvitations([FromQuery] string? status = null)
    {
        var invitations = await _mediator.Send(new GetInvitationsRequest(status));
        return invitations.Succeeded ? Ok(invitations) : BadRequest(invitations);
    }

    [HttpPost("invitations/{id}/revoke")]
    [RequirePermission(Permissions.InviteUsers)]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeInvitation(string id)
    {
        var result = await _mediator.Send(new RevokeInvitationRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("invitations/{id}/resend")]
    [RequirePermission(Permissions.InviteUsers)]
    public async Task<ActionResult<ApiResponse<UserDto>>> ResendInvitation(string id)
    {
        var result = await _mediator.Send(new ResendInvitationRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewUsers)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetAllUsers(int pageSize = 10,
                                                                         int pageNumber = 1,
                                                                         Guid? roleId = null,
                                                                         [FromQuery] List<string>? authUserIds = null,
                                                                         [FromQuery] List<Guid>? regionIds = null,
                                                                         [FromQuery] string? search = null)
    {
        var users = await _mediator.Send(new GetAllUsersRequest(pageSize, pageNumber, roleId, authUserIds, regionIds, search));
        return users.Succeeded ? Ok(users) : BadRequest(users);
    }

    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewUsers)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id)
    {
        var user = await _mediator.Send(new GetUserByIdRequest(id));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpGet("auth-user/{authUserId}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByAuthUserId(
        string authUserId,
        [FromServices] IUserContextService userContextService,
        [FromServices] IUserAuthorizationService userAuthorizationService)
    {
        var currentAuthUserId = userContextService.GetCurrentUserId();
        var isSelf = string.Equals(currentAuthUserId, authUserId, StringComparison.OrdinalIgnoreCase);

        if (!isSelf)
        {
            var hasPermission = await userAuthorizationService.HasPermissionAsync(Permissions.ViewUsers);
            if (!hasPermission)
            {
                return Forbid();
            }
        }

        var user = await _mediator.Send(new GetUserByAuthUserIdRequest(authUserId));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpPatch("{id}")]
    [RequirePermission(Permissions.UpdateUsers)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, UpdateUserDto userDto)
    {
        var user = await _mediator.Send(new UpdateUserByIdRequest(id, userDto));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteUsers)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
    {
        var user = await _mediator.Send(new DeleteUserByIdRequest(id));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }
}
