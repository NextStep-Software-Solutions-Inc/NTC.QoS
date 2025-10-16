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
[Route("webhooks/clerk")]
public class ClerkWebhookController(IMediator mediator) : ControllerBase
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
    /// <response code="200">Webhook processed successfully</response>
    /// <response code="400">Invalid payload or missing data</response>
    /// <response code="401">Invalid webhook signature</response>
    [HttpPost]
    [AllowAnonymous]
    [ClerkWebhookVerification]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandleWebhook()
    {
        if (HttpContext.Items["ClerkWebhookPayload"] is not ClerkWebhookPayload payload)
        {
            return BadRequest("Payload not available");
        }

        switch (payload.Type)
        {
            case "user.created":
                await HandleUserCreated(payload.Data);
                break;
            case "user.updated":
                await HandleUserUpdated(payload.Data);
                break;
            default:
                // Ignore other events
                break;
        }

        return Ok();
    }

    private async Task HandleUserCreated(ClerkUserData userData)
    {
        var createRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = userData.Id,
            Email = userData.EmailAddresses.FirstOrDefault()?.EmailAddress ?? "",
            FirstName = userData.FirstName,
            LastName = userData.LastName,
            RoleId = Guid.Empty,
            OrganizationId = Guid.Empty 
        });

        await _mediator.Send(createRequest);
    }

    private async Task HandleUserUpdated(ClerkUserData userData)
    {
        // Find user by AuthUserId
        var userQuery = new GetUserByAuthUserIdRequest(userData.Id);
        var userResponse = await _mediator.Send(userQuery);
        if (!userResponse.Succeeded || userResponse.Data == null)
        {
            // User not found, perhaps create
            await HandleUserCreated(userData);
            return;
        }

        var updateRequest = new UpdateUserByIdRequest(userResponse.Data.Id, new UpdateUserDto
        {
            AuthUserId = userData.Id,
            FirstName = userData.FirstName,
            LastName = userData.LastName,
            Email = userData.EmailAddresses.FirstOrDefault()?.EmailAddress ?? ""
        });

        await _mediator.Send(updateRequest);
    }
}