using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class CreateUserHandler(IRepository<Domain.Entities.User> userRepository,
                                   IRepository<Domain.Entities.Role> roleRepository,
                                   IRepository<Domain.Entities.Organization> organizationRepository,
                                   IUserRegionRepository userRegionRepository,
                                   IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<CreateUserRequest, ApiResponse<UserDto>>
{
    private readonly IRepository<Domain.Entities.User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<ApiResponse<UserDto>> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Step 1: Check if user already exists
        var existingUser = await _userRepository.FirstOrDefaultAsync(_userRepository.Query(u => u.AuthUserId == request.User.AuthUserId), cancellationToken);

        if (existingUser != null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "User already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["A user with this AuthUserId already exists."] }
                }
            };
        }

        // Step 2: Get default role if not provided
        Domain.Entities.Role? defaultRole = null;
        if (request.User.RoleId == Guid.Empty)
        {
            defaultRole = await _roleRepository.FirstOrDefaultAsync(_roleRepository.Query(r => r.Key == "user"), cancellationToken);

            if (defaultRole == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Default user role key not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Role", ["Default 'user' role key not found in database"] }
                    }
                };
            }
        }

        // Step 3: Get default organization if not provided
        Domain.Entities.Organization? defaultOrg = null;
        if (request.User.OrganizationId == Guid.Empty)
        {
            defaultOrg = await _organizationRepository.FirstOrDefaultAsync(_organizationRepository.Query(o => o.Name == "public"), cancellationToken);

            if (defaultOrg == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Default public organization name not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Organization", ["Default 'public' organization name not found in database"] }
                    }
                };
            }
        }

        // Step 4: Validate explicit role if provided
        Domain.Entities.Role? providedRole = null;
        if (request.User.RoleId != Guid.Empty)
        {
            providedRole = await _roleRepository.FirstOrDefaultAsync(_roleRepository.Query(r => r.Id == request.User.RoleId), cancellationToken);

            if (providedRole == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Role not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "RoleId", ["Role with the specified Id does not exist"] }
                    }
                };
            }
        }

        // Step 5: Validate explicit organization if provided
        if (request.User.OrganizationId != Guid.Empty)
        {
            var providedOrg = await _organizationRepository.FirstOrDefaultAsync(_organizationRepository.Query(o => o.Id == request.User.OrganizationId), cancellationToken);

            if (providedOrg == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Organization not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "OrganizationId", ["Organization with the specified Id does not exist."] }
                    }
                };
            }
        }

        // Step 6: Assign final IDs
        Guid roleId = request.User.RoleId == Guid.Empty ? defaultRole!.Id : request.User.RoleId;
        Guid organizationId = request.User.OrganizationId == Guid.Empty ? defaultOrg!.Id : request.User.OrganizationId;

        // Step 7: Create the user
        var newUser = new Domain.Entities.User
        {
            RoleId = roleId,
            OrganizationId = organizationId,
            AuthUserId = request.User.AuthUserId,
            Email = request.User.Email,
            FirstName = request.User.FirstName,
            LastName = request.User.LastName
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Step 8: Auto-assign active regions if user has SuperAdmin role
        var effectiveRole = providedRole ?? defaultRole;
        if (effectiveRole != null && (string.Equals(effectiveRole.Key, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) || string.Equals(effectiveRole.Key, "super-admin", StringComparison.OrdinalIgnoreCase)))
        {
            await AssignAllRegionsToUserAsync(newUser.Id, cancellationToken);
        }

        // Step 9: Return success response
        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = newUser.Id,
                Role = new()
                {
                    Id = newUser.Role?.Id ?? Guid.Empty,
                    Name = newUser.Role?.Name ?? string.Empty,
                    Key = newUser.Role?.Key ?? string.Empty,
                    Description = newUser.Role?.Description ?? string.Empty
                },
                Organization = new()
                {
                    Id = newUser.Organization?.Id ?? Guid.Empty,
                    Name = newUser.Organization?.Name ?? string.Empty,
                    Description = newUser.Organization?.Description ?? string.Empty
                },
                AuthUserId = newUser.AuthUserId,
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName
            },
            Message = "User created successfully.",
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
