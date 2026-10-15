using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class InviteUserHandler(
    IUserRepository userRepository,
    IRepository<Domain.Entities.Role> roleRepository,
    IRepository<Domain.Entities.Region> regionRepository,
    IClerkUserService clerkUserService,
    IConfiguration configuration,
    IUserAuthorizationService authorizationService,
    ILogger<InviteUserHandler>? logger = null) : IRequestHandler<InviteUserRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IUserAuthorizationService _authorizationService = authorizationService;
    private readonly ILogger<InviteUserHandler>? _logger = logger;

    public async Task<ApiResponse<UserDto>> Handle(InviteUserRequest request, CancellationToken cancellationToken)
    {
        // Check if user already exists in DB with this email
        var existingUserQuery = _userRepository.Query(u => u.Email.ToLower() == request.User.Email.ToLower() && !u.IsDeleted);
        var existingUser = await _userRepository.FirstOrDefaultAsync(existingUserQuery, cancellationToken);
        if (existingUser is not null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "A user with this email address already exists in the system.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["A user with this email address already exists in the system."] }
                }
            };
        }

        var roleQuery = _roleRepository.Query(r => r.Id == request.User.RoleId);
        var role = await _roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);
        if (role is null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Role not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "NullRole", ["Role with the specified Id does not exist"] }
                }
            };
        }

        // Privilege and jurisdiction checks for regional inviters
        var currentUser = await _authorizationService.GetCurrentUserAsync(cancellationToken);
        var isPlatformCaller = currentUser?.Role != null && Roles.IsPlatformRole(currentUser.Role.Key);

        if (!isPlatformCaller)
        {
            if (Roles.IsPlatformRole(role.Key) || string.Equals(role.Scope, Roles.ScopePlatform, StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Regional administrators cannot invite personnel with platform-level roles."
                };
            }

            var accessibleRegions = await _authorizationService.GetAccessibleRegionsAsync(cancellationToken);

            if (request.User.Regions is null || request.User.Regions.Count == 0)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Regional administrators must assign invited personnel to their regional jurisdiction."
                };
            }

            var targetRegionIds = request.User.Regions.Select(r => r.RegionId).Distinct().ToList();
            if (targetRegionIds.Any(id => !accessibleRegions.Contains(id)))
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "You can only invite personnel to regions within your regional jurisdiction."
                };
            }
        }

        // Validate any target regions provided
        var regionAssignments = new List<object>();
        if (request.User.Regions is { Count: > 0 })
        {
            var regionIds = request.User.Regions.Select(r => r.RegionId).Distinct().ToList();
            var validRegionsQuery = _regionRepository.Query(r => regionIds.Contains(r.Id) && !r.IsDeleted);
            var validRegions = await _regionRepository.ToListAsync(validRegionsQuery, cancellationToken);
            var validRegionIdSet = validRegions.Select(r => r.Id).ToHashSet();

            foreach (var r in request.User.Regions.GroupBy(r => r.RegionId).Select(g => g.First()))
            {
                if (!validRegionIdSet.Contains(r.RegionId))
                {
                    return new ApiResponse<UserDto>
                    {
                        Succeeded = false,
                        Message = $"Target region with ID {r.RegionId} was not found."
                    };
                }

                Guid regionalRoleId = r.RoleId ?? role.Id;
                if (r.RoleId.HasValue && r.RoleId.Value != role.Id)
                {
                    var regionalRoleQuery = _roleRepository.Query(rr => rr.Id == r.RoleId.Value);
                    var regionalRole = await _roleRepository.FirstOrDefaultAsync(regionalRoleQuery, cancellationToken);
                    if (regionalRole is null)
                    {
                        return new ApiResponse<UserDto>
                        {
                            Succeeded = false,
                            Message = $"Regional role with ID {r.RoleId.Value} was not found."
                        };
                    }
                    regionalRoleId = regionalRole.Id;
                }

                regionAssignments.Add(new
                {
                    region_id = r.RegionId.ToString(),
                    role_id = regionalRoleId.ToString(),
                    status = string.IsNullOrWhiteSpace(r.Status) ? "Active" : r.Status
                });
            }
        }

        // Determine redirect URL: client-supplied origin or configured fallback
        string redirectUrl;
        if (!string.IsNullOrWhiteSpace(request.User.RedirectUrl))
        {
            redirectUrl = request.User.RedirectUrl;
        }
        else
        {
            var baseUrl = _configuration["Clerk:FrontendUrl"]
                ?? _configuration.GetSection("Clerk:AuthorizedParties").Get<string[]>()?.FirstOrDefault()
                ?? "http://localhost:3000";

            baseUrl = baseUrl.TrimEnd('/');
            redirectUrl = baseUrl.EndsWith("/select-region", StringComparison.OrdinalIgnoreCase)
                ? baseUrl
                : $"{baseUrl}/select-region";
        }

        var invitationPayload = new CreateClerkInvitationPayload
        {
            EmailAddress = request.User.Email,
            RedirectUrl = redirectUrl,
            PublicMetadata = new
            {
                role_id = role.Id.ToString(),
                regions = regionAssignments
            },
            Notify = true,
            IgnoreExisting = true
        };

        // Issue the new invitation via Clerk first (with IgnoreExisting = true)
        // This ensures that if Clerk API fails, existing invitations are NOT prematurely revoked
        ClerkInvitationResponse newInvitation;
        try
        {
            newInvitation = await _clerkUserService.CreateInvitationAsync(invitationPayload);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send invitation via Clerk for {Email}: {Message}", StringHelper.MaskEmail(request.User.Email), StringHelper.SanitizeLogMessage(ex.Message));
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = $"Failed to send invitation via Clerk: {ex.Message}",
                Errors = new Dictionary<string, string[]>
                {
                    { "Clerk", [ex.Message] }
                }
            };
        }

        // Only after the new invitation is successfully created, clean up prior older pending invitations for this email
        try
        {
            var existingInvitations = await _clerkUserService.GetInvitationsAsync("pending");
            var newInvitationId = newInvitation?.Id;
            var priorInvites = existingInvitations
                .Where(i => string.Equals(i.EmailAddress, request.User.Email, StringComparison.OrdinalIgnoreCase) &&
                            (newInvitationId == null || !string.Equals(i.Id, newInvitationId, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var priorInvite in priorInvites)
            {
                try
                {
                    await _clerkUserService.RevokeInvitationAsync(priorInvite.Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to revoke prior pending invitation {InvitationId} for {Email}", priorInvite.Id, StringHelper.MaskEmail(request.User.Email));
                }
            }
        }
        catch (Exception ex)
        {
            // Non-fatal: if cleanup of old invites fails, the fresh invitation is already active
            _logger?.LogWarning(ex, "Failed to query or clean up prior pending invitations for {Email}", StringHelper.MaskEmail(request.User.Email));
        }

        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = Guid.Empty,
                AuthUserId = string.Empty,
                Email = request.User.Email,
                Role = new()
                {
                    Id = role.Id,
                    Name = role.Name,
                    Key = role.Key,
                    Description = role.Description
                }
            },
            Message = "Invitation sent successfully via Clerk. The user account will be created once they accept.",
            Succeeded = true
        };
    }
}
