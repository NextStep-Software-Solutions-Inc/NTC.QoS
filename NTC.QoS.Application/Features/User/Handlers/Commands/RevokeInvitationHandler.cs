using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class RevokeInvitationHandler(
    IClerkUserService clerkUserService,
    ILogger<RevokeInvitationHandler> logger) : IRequestHandler<RevokeInvitationRequest, ApiResponse<bool>>
{
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly ILogger<RevokeInvitationHandler> _logger = logger;

    public async Task<ApiResponse<bool>> Handle(RevokeInvitationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InvitationId))
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Invitation ID is required.",
                Errors = new Dictionary<string, string[]>
                {
                    { "InvitationId", ["Invitation ID cannot be empty."] }
                }
            };
        }

        try
        {
            var response = await _clerkUserService.RevokeInvitationAsync(request.InvitationId);
            return new ApiResponse<bool>
            {
                Data = true,
                Succeeded = true,
                Message = $"Invitation for {response.EmailAddress} revoked successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke invitation {InvitationId}: {Message}", request.InvitationId, ex.Message);
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = $"Failed to revoke invitation: {ex.Message}",
                Errors = new Dictionary<string, string[]>
                {
                    { "Revoke", [ex.Message] }
                }
            };
        }
    }
}
