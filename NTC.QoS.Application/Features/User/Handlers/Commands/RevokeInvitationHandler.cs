using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class RevokeInvitationHandler(
    IClerkUserService clerkUserService,
    IUserRepository userRepository,
    IUserRegionRepository userRegionRepository,
    ILogger<RevokeInvitationHandler> logger) : IRequestHandler<RevokeInvitationRequest, ApiResponse<bool>>
{
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
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

            // Clean up any un-onboarded local database user created during incomplete sign-in
            if (!string.IsNullOrEmpty(response?.EmailAddress))
            {
                try
                {
                    var userQuery = _userRepository.Query(u => u.Email.ToLower() == response.EmailAddress.ToLower() && !u.IsDeleted)
                        .Include(u => u.Role);
                    var existingUser = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);
                    if (existingUser != null)
                    {
                        var isDefaultRole = existingUser.Role == null || Roles.IsUser(existingUser.Role.Key);
                        var regQuery = _userRegionRepository.Query(ur => ur.UserId == existingUser.Id && !ur.IsDeleted);
                        var hasRegions = await _userRegionRepository.CountAsync(regQuery, cancellationToken) > 0;

                        if (isDefaultRole && !hasRegions)
                        {
                            _logger.LogInformation("Cleaning up un-onboarded user {UserId} ({Email}) after invitation {InvitationId} revocation",
                                existingUser.Id, StringHelper.MaskEmail(response.EmailAddress), request.InvitationId);
                            _userRepository.Delete(existingUser);
                            await _userRepository.SaveChangesAsync(cancellationToken);
                        }
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up un-onboarded user for revoked invitation {InvitationId} ({Email})",
                        request.InvitationId, StringHelper.MaskEmail(response?.EmailAddress));
                }
            }

            return new ApiResponse<bool>
            {
                Data = true,
                Succeeded = true,
                Message = $"Invitation for {response?.EmailAddress} revoked successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke invitation {InvitationId}: {Message}", request.InvitationId, StringHelper.SanitizeLogMessage(ex.Message));
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
