using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class UpdateUserByIdHandler(IUserRepository userRepository,
                                   IRepository<Domain.Entities.Role> roleRepository,
                                   IRepository<Domain.Entities.Organization> organizationRepository,
                                   IUserRegionRepository userRegionRepository,
                                   IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<UpdateUserByIdRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<ApiResponse<UserDto>> Handle(UpdateUserByIdRequest request, CancellationToken cancellationToken)
    {
        var userQuery = _userRepository.Query(r => r.Id == request.Id);
        var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);
        if (user is null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["User with the specified ID does not exist."]
                    }
                },
                Message = "User not found."
            };
        }

        Domain.Entities.Role? role = null;
        if (request.User.RoleId.HasValue)
        {
            var roleQuery = _roleRepository.Query(r => r.Id == request.User.RoleId.Value);
            role = await _roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);
            if (role is null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Role not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "RoleId", ["Role with the specified ID does not exist."]
                        }
                    }
                };
            }
            user.RoleId = role.Id;
        }

        if (request.User.OrganizationId.HasValue)
        {
            var organizationQuery = _organizationRepository.Query(r => r.Id == request.User.OrganizationId.Value);
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
                            "OrganizationId", ["Organization with the specified ID does not exist."]
                        }
                    }
                };
            }
            user.OrganizationId = organization.Id;
        }

        if (!string.IsNullOrEmpty(request.User.AuthUserId)) user.AuthUserId = request.User.AuthUserId;

        var existingUserQuery = _userRepository.Query(u => u.Id != request.Id &&
                                 u.RoleId == user.RoleId &&
                                 u.OrganizationId == user.OrganizationId &&
                                 u.AuthUserId == user.AuthUserId);
        var existingUser = await _userRepository.FirstOrDefaultAsync(existingUserQuery, cancellationToken);
        if (existingUser != null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Duplicate user found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["User with the same Role, Organization, and AuthUserId already exists."]
                    }
                }
            };
        }
        user.Email = request.User.Email ?? user.Email;
        user.FirstName = request.User.FirstName ?? user.FirstName;
        user.LastName = request.User.LastName ?? user.LastName;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Auto-assign active regions if user is or became SuperAdmin
        var effectiveRole = role;
        if (effectiveRole == null && user.RoleId != Guid.Empty)
        {
            var roleQuery = _roleRepository.Query(r => r.Id == user.RoleId);
            effectiveRole = await _roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);
        }

        if (effectiveRole != null && (string.Equals(effectiveRole.Key, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) || string.Equals(effectiveRole.Key, "super-admin", StringComparison.OrdinalIgnoreCase)))
        {
            await AssignAllRegionsToUserAsync(user.Id, cancellationToken);
        }

        var result = new UserDto
        {
            Id = user.Id,
            Role = new()
            {
                Id = user.RoleId,
                Name = user.Role?.Name ?? string.Empty,
                Key = user.Role?.Key ?? string.Empty,
                Description = user.Role?.Description ?? string.Empty
            },
            Organization = new()
            {
                Id = user.OrganizationId,
                Name = user.Organization?.Name ?? string.Empty,
                Description = user.Organization?.Description ?? string.Empty
            },
            AuthUserId = user.AuthUserId,
            Email = user.Email,
        };

        return new ApiResponse<UserDto>
        {
            Data = result,
            Message = "User updated successfully.",
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

