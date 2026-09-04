using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class InviteUserHandler(IUserRepository userRepository,
                               IRepository<Domain.Entities.Role> roleRepository,
                               IRepository<Domain.Entities.Organization> organizationRepository,
                               IUserRegionRepository userRegionRepository,
                               IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<InviteUserRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<ApiResponse<UserDto>> Handle(InviteUserRequest request, CancellationToken cancellationToken)
    {
        // Check for exact duplicate: same email, role, and organization
        // This allows the same email to be invited to different roles or organizations
        // which may be intentional for multi-tenant or multi-role scenarios
        var existingUserQuery = _userRepository.Query(u => u.Email == request.User.Email
                                                           && u.RoleId == request.User.RoleId
                                                           && u.OrganizationId == request.User.OrganizationId);
        var existingUser = await _userRepository.FirstOrDefaultAsync(existingUserQuery, cancellationToken);
        if (existingUser is not null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Duplicate user found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["User with the same Email, Role, and Organization already exists."]
                    }
                }
            };
        }

        // Optional: Add global email uniqueness check if business rules require it
        // Uncomment the following lines if email should be globally unique across all roles/organizations

        // var emailExistsQuery = _userRepository.Query(u => u.Email == request.User.Email);
        // var emailExists = await _userRepository.FirstOrDefaultAsync(emailExistsQuery, cancellationToken);
        // if (emailExists is not null)
        // {
        //     return new ApiResponse<UserDto>
        //     {
        //         Succeeded = false,
        //         Message = "Email already exists.",
        //         Errors = new Dictionary<string, string[]>
        //         {
        //             { 
        //                 "Email", ["A user with this email address already exists in the system."] 
        //             }
        //         }
        //     };
        // }

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
                    {
                        "NullRole", ["Role with the specified Id does not exist"]
                    }
                }
            };
        }

        var organizationQuery = _organizationRepository.Query(r => r.Id == request.User.OrganizationId);
        var organization = await _organizationRepository.FirstOrDefaultAsync(organizationQuery, cancellationToken);
        if (organization is null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Organization not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullOrganization", ["Organization with the specified Id does not exist."]
                    }
                }
            };
        }

        var newUser = new Domain.Entities.User
        {
            Email = request.User.Email,
            RoleId = request.User.RoleId,
            OrganizationId = request.User.OrganizationId,
            AuthUserId = string.Empty,
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Auto-assign active regions if user has SuperAdmin role
        if (string.Equals(role.Key, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) || string.Equals(role.Key, "super-admin", StringComparison.OrdinalIgnoreCase))
        {
            await AssignAllRegionsToUserAsync(newUser.Id, cancellationToken);
        }

        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = newUser.Id,
                Role = new()
                {
                    Id = role.Id,
                    Name = role.Name,
                    Key = role.Key,
                    Description = role.Description
                },
                Organization = new()
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    Description = organization.Description
                },
                AuthUserId = newUser.AuthUserId,
                Email = newUser.Email
            },
            Message = "User invitation created successfully.",
            Succeeded = true
        };
    }

    private async Task AssignAllRegionsToUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var allRegionsQuery = _regionRepository.Query(r => !r.IsDeleted);
        var allRegions = await _regionRepository.ToListAsync(allRegionsQuery, cancellationToken);

        if (allRegions.Count == 0) return;

        var existingUserRegionsQuery = _userRegionRepository.Query(ur => ur.UserId == userId && !ur.IsDeleted);
        var existingUserRegions = await _userRegionRepository.ToListAsync(existingUserRegionsQuery, cancellationToken);

        var hasUpdates = false;
        foreach (var existing in existingUserRegions)
        {
            if (existing.Status != UserRegionStatus.Active.ToString())
            {
                existing.Status = UserRegionStatus.Active.ToString();
                _userRegionRepository.Update(existing);
                hasUpdates = true;
            }
        }

        var existingRegionIds = existingUserRegions.Select(ur => ur.RegionId).ToHashSet();
        var missingRegions = allRegions.Where(r => !existingRegionIds.Contains(r.Id)).ToList();

        if (missingRegions.Count > 0)
        {
            var newUserRegions = missingRegions.Select(region => new Domain.Aggregates.UserRegion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RegionId = region.Id,
                Status = UserRegionStatus.Active.ToString(),
            }).ToList();

            _userRegionRepository.AddRange(newUserRegions);
            hasUpdates = true;
        }

        if (hasUpdates)
        {
            await _userRegionRepository.SaveChangesAsync(cancellationToken);
        }
    }
}