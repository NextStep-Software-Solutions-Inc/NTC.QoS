using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.GenerationBand.Requests;
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

}
