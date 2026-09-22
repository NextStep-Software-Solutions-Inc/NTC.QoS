using MediatR;
using Microsoft.Extensions.Configuration;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class InviteUserHandler(
    IUserRepository userRepository,
    IRepository<Domain.Entities.Role> roleRepository,
    IRepository<Domain.Entities.Region> regionRepository,
    IClerkUserService clerkUserService,
    IConfiguration configuration) : IRequestHandler<InviteUserRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly IConfiguration _configuration = configuration;

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
        var redirectUrl = !string.IsNullOrWhiteSpace(request.User.RedirectUrl)
            ? request.User.RedirectUrl
            : _configuration["Clerk:FrontendUrl"]
              ?? _configuration.GetSection("Clerk:AuthorizedParties").Get<string[]>()?.FirstOrDefault()
              ?? "http://localhost:3000/select-region";

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

        try
        {
            await _clerkUserService.CreateInvitationAsync(invitationPayload);
        }
        catch (Exception ex)
        {
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
