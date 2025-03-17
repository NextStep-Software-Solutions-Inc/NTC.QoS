using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("users", Name = "users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateUserDto>>> CreateUser(CreateUserDto userDto)
    {
        var user = await _mediator.Send(new CreateUserRequest
        {
            User = userDto
        });
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<GetUserDto>>> GetAllUsers(int pageSize = 10,
                                                                         int pageNumber = 1,
                                                                         Guid? roleId = null,
                                                                         Guid? organizationId = null)
    {
        var users = await _mediator.Send(new GetAllUsersRequest(pageSize, pageNumber, roleId, organizationId));
        return users.Succeeded ? Ok(users) : BadRequest(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GetUserDto>>> GetUserById(Guid id)
    {
        var user = await _mediator.Send(new GetUserByIdRequest(id));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateUserDto>>> UpdateUser(Guid id, UpdateUserDto updateUserDto)
    {
        var user = await _mediator.Send(new UpdateUserByIdRequest(id, updateUserDto));
        return user.Succeeded ? Ok(user) : BadRequest(user);
    }
}
