using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Filters;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Handles Clerk authentication webhooks for user management
/// </summary>
[ApiController]
[Route("webhooks")]
public class WebhookController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Processes Clerk webhook events for user creation and updates
    /// </summary>
    /// <remarks>
    /// This endpoint receives webhook events from Clerk authentication service.
    /// Supported events:
    /// - user.created: Creates a new user in the system
    /// - user.updated: Updates an existing user's information
    ///
    /// The webhook payload is automatically verified using Svix signature verification.
    /// </remarks>
    /// <returns>OK response if the webhook was processed successfully</returns>
    [HttpPost("clerk")]
    [AllowAnonymous]
    [ClerkWebhookVerification]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleWebhook()
    {
        if (HttpContext.Items["ClerkWebhookPayload"] is not ClerkWebhookPayload payload)
        {
            return BadRequest("Payload not available");
        }

        if (payload.Type is null || payload.Data is null)
        {
            return BadRequest("Invalid webhook payload structure");
        }

        var response = new Domain.Models.DTOs.ApiResponse<UserDto>();
        switch (payload.Type)
        {
            case "user.created":
                response = await HandleUserCreated(payload.Data);
                break;
            case "user.updated":
                response = await HandleUserUpdated(payload.Data);
                break;
            default:
                // Ignore other events
                response = new Domain.Models.DTOs.ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "No action taken for this event type."
                };
                break;
        }

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleUserCreated(ClerkUserData userData)
    {
        if (userData.Id is null || userData.FirstName is null || userData.LastName is null)
        {
            // Log or handle invalid user data
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data in webhook payload."
            };
        }

        var createRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = userData.Id,
            Email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? "",
            FirstName = userData.FirstName,
            LastName = userData.LastName,
            RoleId = Guid.Empty,
            OrganizationId = Guid.Empty
        });

        return await _mediator.Send(createRequest);

    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleUserUpdated(ClerkUserData userData)
    {
        if (userData.Id is null)
        {
            // Log or handle invalid user data
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data in webhook payload."
            };
        }

        // Find user by AuthUserId
        var userQuery = new GetUserByAuthUserIdRequest(userData.Id);
        var userResponse = await _mediator.Send(userQuery);
        if (!userResponse.Succeeded || userResponse.Data == null)
        {
            // User not found, perhaps create
            return await HandleUserCreated(userData);
            
        }

        var updateRequest = new UpdateUserByIdRequest(userResponse.Data.Id, new UpdateUserDto
        {
            AuthUserId = userData.Id,
            FirstName = userData.FirstName ?? "",
            LastName = userData.LastName ?? "",
            Email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? ""
        });

        return await _mediator.Send(updateRequest);
    }
}