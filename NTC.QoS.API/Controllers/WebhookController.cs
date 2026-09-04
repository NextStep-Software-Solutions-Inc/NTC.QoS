using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Filters;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Handles Clerk authentication webhooks for user management
/// </summary>
[ApiController]
[Route("webhooks")]
public class WebhookController(IMediator mediator, IUserProvisioningService userProvisioningService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IUserProvisioningService _userProvisioningService = userProvisioningService;

    /// <summary>
    /// Processes Clerk webhook events for user creation and updates
    /// </summary>
    /// <remarks>
    /// This endpoint receives webhook events from Clerk authentication service.
    /// Supported events:
    /// - user.created: Creates a new user in the system
    /// - user.updated: Updates an existing user's information
    /// - session.created: Ensures user exists and synchronizes session metadata
    /// - session.ended: Handles session termination
    /// - session.removed: Handles session cleanup
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

        if (string.IsNullOrEmpty(payload.Type) || payload.Data is null)
        {
            return BadRequest("Invalid webhook payload structure");
        }

        var response = payload.Type switch
        {
            "user.created" or "user.updated" => await HandleUserWebhook(payload.Data),
            "session.created" => await HandleSessionCreated(payload.Data),
            "session.ended" => await HandleSessionEnded(payload.Data),
            "session.removed" => await HandleSessionRemoved(payload.Data),
            _ => new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = true,
                Message = $"No action required for event type '{payload.Type}'."
            },
        };

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleUserWebhook(object userDataObj)
    {
        if (userDataObj is not ClerkUserData userData)
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data."
            };
        }

        return await _userProvisioningService.UpsertUserFromWebhookAsync(userData);
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleSessionCreated(object sessionData)
    {
        if (sessionData is not ClerkSessionData data)
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid session data."
            };
        }

        if (string.IsNullOrWhiteSpace(data.UserId))
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid session data: missing user ID."
            };
        }

        // Ensure user exists in DB (creates if not existing and syncs metadata)
        var ensureUserResult = await _userProvisioningService.EnsureUserExistsAsync(data.UserId);
        if (!ensureUserResult.Succeeded)
        {
            return ensureUserResult;
        }

        var createSessionRequest = new CreateSessionRequest
        {
            AuthUserId = data.UserId
        };

        var response = await _mediator.Send(createSessionRequest);

        return new Domain.Models.DTOs.ApiResponse<UserDto>
        {
            Data = ensureUserResult.Data,
            Succeeded = response.Succeeded,
            Message = response.Message
        };
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleSessionEnded(object sessionData)
    {
        if (sessionData is not ClerkSessionData data)
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid session data."
            };
        }

        if (!string.IsNullOrWhiteSpace(data.UserId))
        {
            await _userProvisioningService.EnsureUserExistsAsync(data.UserId);
        }

        return new Domain.Models.DTOs.ApiResponse<UserDto>
        {
            Succeeded = true,
            Message = "Session ended event handled."
        };
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleSessionRemoved(object sessionData)
    {
        if (sessionData is not ClerkSessionData data)
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid session data."
            };
        }

        if (!string.IsNullOrWhiteSpace(data.UserId))
        {
            await _userProvisioningService.EnsureUserExistsAsync(data.UserId);
        }

        return new Domain.Models.DTOs.ApiResponse<UserDto>
        {
            Succeeded = true,
            Message = "Session removed event handled."
        };
    }
}