using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;
using System.Text.Json;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class ResendInvitationHandler(
    IClerkUserService clerkUserService,
    IMediator mediator,
    ILogger<ResendInvitationHandler> logger) : IRequestHandler<ResendInvitationRequest, ApiResponse<UserDto>>
{
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<ResendInvitationHandler> _logger = logger;

    public async Task<ApiResponse<UserDto>> Handle(ResendInvitationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InvitationId))
        {
            return new ApiResponse<UserDto>
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
            var invitations = await _clerkUserService.GetInvitationsAsync("pending");
            var target = invitations.FirstOrDefault(i => string.Equals(i.Id, request.InvitationId, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Pending invitation was not found or has already been accepted/revoked."
                };
            }

            // Extract role_id and regions from metadata
            Guid? roleId = null;
            var regionDtos = new List<InviteUserRegionDto>();

            if (target.PublicMetadata.HasValue && target.PublicMetadata.Value.ValueKind == JsonValueKind.Object)
            {
                var meta = target.PublicMetadata.Value;
                if (meta.TryGetProperty("role_id", out var roleIdProp) &&
                    Guid.TryParse(roleIdProp.GetString(), out var parsedRoleId))
                {
                    roleId = parsedRoleId;
                }

                if (meta.TryGetProperty("regions", out var regionsProp) &&
                    regionsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in regionsProp.EnumerateArray())
                    {
                        if (elem.TryGetProperty("region_id", out var regIdProp) &&
                            Guid.TryParse(regIdProp.GetString(), out var regId))
                        {
                            Guid? regRoleId = null;
                            if (elem.TryGetProperty("role_id", out var regRoleIdProp) &&
                                Guid.TryParse(regRoleIdProp.GetString(), out var parsedRegRoleId))
                            {
                                regRoleId = parsedRegRoleId;
                            }

                            string status = elem.TryGetProperty("status", out var stProp)
                                ? stProp.GetString() ?? "Active"
                                : "Active";

                            regionDtos.Add(new InviteUserRegionDto
                            {
                                RegionId = regId,
                                RoleId = regRoleId,
                                Status = status
                            });
                        }
                    }
                }
            }

            if (!roleId.HasValue)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Cannot resend invitation: original role information is missing. Please issue a new invitation."
                };
            }

            // Delegate to InviteUserHandler via mediator: validates jurisdiction, auto-revokes old pending invites, and issues fresh invite
            var inviteDto = new InviteUserDto
            {
                Email = target.EmailAddress,
                RoleId = roleId.Value,
                Regions = regionDtos
            };

            var result = await _mediator.Send(new InviteUserRequest(inviteDto), cancellationToken);
            if (result.Succeeded)
            {
                result.Message = $"Invitation for {target.EmailAddress} was resent successfully.";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend invitation {InvitationId}: {Message}", request.InvitationId, ex.Message);
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = $"Failed to resend invitation: {ex.Message}",
                Errors = new Dictionary<string, string[]>
                {
                    { "Resend", [ex.Message] }
                }
            };
        }
    }
}
