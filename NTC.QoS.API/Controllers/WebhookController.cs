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
public class WebhookController(IMediator mediator, IClerkUserService clerkUserService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IClerkUserService _clerkUserService = clerkUserService;

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

        if (string.IsNullOrEmpty(payload.Type) || payload.Data is null)
        {
            return BadRequest("Invalid webhook payload structure");
        }

        var response = payload.Type switch
        {
            "user.created" => await HandleUserCreated(payload.Data),
            "user.updated" => await HandleUserUpdated(payload.Data),
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

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> EnsureUserExists(string authUserId)
    {
        var userQuery = new GetUserByAuthUserIdRequest(authUserId);
        var userResponse = await _mediator.Send(userQuery);
        if (userResponse.Succeeded && userResponse.Data != null)
        {
            return userResponse;
        }

        // User not found in DB, fetch from Clerk and create
        var clerkUser = await _clerkUserService.GetUserByIdAsync(authUserId);
        if (clerkUser == null)
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = $"User with AuthUserId '{authUserId}' not found in Clerk."
            };
        }

        var createUserRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = clerkUser.Id,
            Email = clerkUser.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? "",
            FirstName = clerkUser.FirstName ?? "",
            LastName = clerkUser.LastName ?? "",
            RoleId = Guid.Empty,
            OrganizationId = Guid.Empty
        });

        return await _mediator.Send(createUserRequest);
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleUserCreated(object userDataObj)
    {
        if (userDataObj is not ClerkUserData userData)
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data."
            };
        }

        if (string.IsNullOrWhiteSpace(userData.Id))
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data in webhook payload: missing user ID."
            };
        }

        // Check if user already exists
        var userQuery = new GetUserByAuthUserIdRequest(userData.Id);
        var userResponse = await _mediator.Send(userQuery);
        if (userResponse.Succeeded && userResponse.Data != null)
        {
            // User already exists, update instead of failing with duplicate error
            var updateRequest = new UpdateUserByIdRequest(userResponse.Data.Id, new UpdateUserDto
            {
                AuthUserId = userData.Id,
                FirstName = userData.FirstName ?? "",
                LastName = userData.LastName ?? "",
                Email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? ""
            });

            return await _mediator.Send(updateRequest);
        }

        var createRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = userData.Id,
            Email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? "",
            FirstName = userData.FirstName ?? "",
            LastName = userData.LastName ?? "",
            RoleId = Guid.Empty,
            OrganizationId = Guid.Empty
        });

        return await _mediator.Send(createRequest);
    }

    private async Task<Domain.Models.DTOs.ApiResponse<UserDto>> HandleUserUpdated(object userDataObj)
    {
        if (userDataObj is not ClerkUserData userData)
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data."
            };
        }

        if (string.IsNullOrWhiteSpace(userData.Id))
        {
            return new Domain.Models.DTOs.ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data in webhook payload: missing user ID."
            };
        }

        // Find user by AuthUserId
        var userQuery = new GetUserByAuthUserIdRequest(userData.Id);
        var userResponse = await _mediator.Send(userQuery);
        if (!userResponse.Succeeded || userResponse.Data == null)
        {
            // User not found in DB, create user
            return await HandleUserCreated(userDataObj);
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

        // Ensure user exists in DB (creates if not existing)
        var ensureUserResult = await EnsureUserExists(data.UserId);
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
            // Ensure user exists
            await EnsureUserExists(data.UserId);
        }

        // TODO: Implement session ended logic, e.g., update user status or log logout
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
            // Ensure user exists
            await EnsureUserExists(data.UserId);
        }

        // TODO: Implement session removed logic, e.g., cleanup session data
        return new Domain.Models.DTOs.ApiResponse<UserDto>
        {
            Succeeded = true,
            Message = "Session removed event handled."
        };
    }
}