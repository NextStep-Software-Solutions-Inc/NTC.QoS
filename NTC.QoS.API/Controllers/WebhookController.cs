using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Filters;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs.User;

using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Helper;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Handles Clerk authentication webhooks for user management
/// </summary>
[ApiController]
[Route("webhooks")]
public class WebhookController(
    IMediator mediator,
    IUserProvisioningService userProvisioningService,
    ILogger<WebhookController>? logger = null) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IUserProvisioningService _userProvisioningService = userProvisioningService;
    private readonly ILogger<WebhookController>? _logger = logger;

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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleWebhook()
    {
        if (HttpContext.Items["ClerkWebhookPayload"] is not ClerkWebhookPayload payload)
        {
            _logger?.LogWarning("Clerk webhook request rejected: payload not available in HttpContext.Items.");
            return BadRequest("Payload not available");
        }

        if (string.IsNullOrEmpty(payload.Type) || payload.Data is null)
        {
            _logger?.LogWarning("Clerk webhook request rejected: invalid payload structure or empty event type.");
            return BadRequest("Invalid webhook payload structure");
        }

        try
        {
            var response = payload.Type switch
            {
                "user.created" or "user.updated" => await HandleUserWebhook(payload.Type, payload.Data),
                "session.created" => await HandleSessionCreated(payload.Data),
                "session.ended" => await HandleSessionEnded(payload.Data),
                "session.removed" => await HandleSessionRemoved(payload.Data),
                _ => new Domain.Models.DTOs.ApiResponse<UserDto>
                {
                    Succeeded = true,
                    Message = $"No action required for event type '{payload.Type}'."
                },
            };

            if (!response.Succeeded)
            {
                _logger?.LogError("Downstream Clerk webhook processing failed for event '{EventType}': {ErrorMessage}",
                    payload.Type, StringHelper.SanitizeLogMessage(response.Message));
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected exception during Clerk webhook processing for event '{EventType}': {ErrorMessage}",
                payload.Type, StringHelper.SanitizeLogMessage(ex.Message));

            return StatusCode(StatusCodes.Status500InternalServerError, new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = $"An internal error occurred while processing the webhook: {StringHelper.SanitizeLogMessage(ex.Message)}"
            });
        }
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleUserWebhook(string eventType, object userDataObj)
    {
        if (userDataObj is not ClerkUserData userData)
        {
            _logger?.LogError("Failed processing {EventType} webhook: payload data is not ClerkUserData.", eventType);
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data."
            };
        }

        var result = await _userProvisioningService.UpsertUserFromWebhookAsync(userData);
        if (!result.Succeeded)
        {
            _logger?.LogError("Failed processing {EventType} webhook for Clerk user {AuthUserId}: {ErrorMessage}",
                eventType, userData.Id, result.Message);
        }
        return result;
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleSessionCreated(object sessionData)
    {
        if (sessionData is not ClerkSessionData data)
        {
            _logger?.LogError("Failed processing session.created webhook: payload data is not ClerkSessionData.");
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid session data."
            };
        }

        if (string.IsNullOrWhiteSpace(data.UserId))
        {
            _logger?.LogError("Failed processing session.created webhook for session {SessionId}: missing user ID.", data.Id);
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
            _logger?.LogError("JIT provisioning failed during session.created webhook for user {AuthUserId} (Session: {SessionId}): {ErrorMessage}",
                data.UserId, data.Id, ensureUserResult.Message);
            return ensureUserResult;
        }

        var createSessionRequest = new CreateSessionRequest
        {
            AuthUserId = data.UserId
        };

        var response = await _mediator.Send(createSessionRequest);

        if (!response.Succeeded)
        {
            _logger?.LogError("Session synchronization failed during session.created webhook for user {AuthUserId} (Session: {SessionId}): {ErrorMessage}",
                data.UserId, data.Id, response.Message);
        }

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