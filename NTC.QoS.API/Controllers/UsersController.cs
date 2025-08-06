using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("users", Name = "users")]
[Authorize]

public class UsersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto userDto)
    {
        var user = await _mediator.Send(new CreateUserRequest(userDto));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetAllUsers(int pageSize = 10,
                                                                         int pageNumber = 1,
                                                                         Guid? roleId = null,
                                                                         Guid? organizationId = null,
                                                                         [FromQuery] List<Guid>? regionIds = null)
    {
        var users = await _mediator.Send(new GetAllUsersRequest(pageSize, pageNumber, roleId, organizationId, regionIds));
        return users.Succeeded ? Ok(users) : BadRequest(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id)
    {
        var user = await _mediator.Send(new GetUserByIdRequest(id));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, UpdateUserDto userDto)
    {
        var user = await _mediator.Send(new UpdateUserByIdRequest(id, userDto));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
    {
        var user = await _mediator.Send(new DeleteUserByIdRequest(id));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }
}
