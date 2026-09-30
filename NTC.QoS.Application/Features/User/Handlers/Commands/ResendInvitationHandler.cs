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

            // Extract role_id and regions from metadata safely
            Guid? roleId = null;
            var regionDtos = new List<InviteUserRegionDto>();

            try
            {
                if (target.PublicMetadata.HasValue && target.PublicMetadata.Value.ValueKind == JsonValueKind.Object)
                {
                    var meta = target.PublicMetadata.Value;

                    // Safely extract primary role_id
                    if (meta.TryGetProperty("role_id", out var roleIdProp) &&
                        roleIdProp.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(roleIdProp.GetString(), out var parsedRoleId))
                    {
                        roleId = parsedRoleId;
                    }

                    // Safely extract regional assignments
                    if (meta.TryGetProperty("regions", out var regionsProp) &&
                        regionsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in regionsProp.EnumerateArray())
                        {
                            if (elem.ValueKind == JsonValueKind.Object &&
                                elem.TryGetProperty("region_id", out var regIdProp) &&
                                regIdProp.ValueKind == JsonValueKind.String &&
                                Guid.TryParse(regIdProp.GetString(), out var regId))
                            {
                                Guid? regRoleId = null;
                                if (elem.TryGetProperty("role_id", out var regRoleIdProp) &&
                                    regRoleIdProp.ValueKind == JsonValueKind.String &&
                                    Guid.TryParse(regRoleIdProp.GetString(), out var parsedRegRoleId))
                                {
                                    regRoleId = parsedRegRoleId;
                                }

                                string status = "Active";
                                if (elem.TryGetProperty("status", out var stProp) &&
                                    stProp.ValueKind == JsonValueKind.String)
                                {
                                    status = stProp.GetString() ?? "Active";
                                }

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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Public metadata parsing encountered an unexpected structure for invitation {InvitationId}", request.InvitationId);
            }

            // Fallback: If role_id was not in metadata or structure drifted, recover from local database if existing user row exists
            if (!roleId.HasValue && !string.IsNullOrEmpty(target.EmailAddress))
            {
                try
                {
                    var userLookup = await _mediator.Send(new GetAllUsersRequest(pageSize: 1, pageNumber: 1, email: target.EmailAddress), cancellationToken);
                    var matchedUser = userLookup?.Data?.FirstOrDefault();
                    if (matchedUser?.Role != null)
                    {
                        roleId = matchedUser.Role.Id;
                        _logger.LogInformation("Recovered role {RoleId} from database for resending invitation to {Email}", roleId, target.EmailAddress);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to recover role from database for invitation {InvitationId}", request.InvitationId);
                }
            }

            if (!roleId.HasValue)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = $"Cannot resend invitation for {target.EmailAddress}: role assignment information is missing. The existing invitation remains intact. Please issue a new invitation."
                };
            }

            // Delegate to InviteUserHandler via mediator: validates jurisdiction, creates new invite first, and only then revokes older pending invites
            var inviteDto = new InviteUserDto
            {
                Email = target.EmailAddress,
                RoleId = roleId.Value,
                Regions = regionDtos
            };

            var result = await _mediator.Send(new InviteUserRequest(inviteDto), cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Resend creation failed for {Email}: {Message}. Original invitation was preserved.", target.EmailAddress, result.Message);
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = $"Failed to issue replacement invitation for {target.EmailAddress}: {result.Message}. The original invitation remains active.",
                    Errors = result.Errors
                };
            }

            result.Message = $"Invitation for {target.EmailAddress} was resent successfully.";
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
