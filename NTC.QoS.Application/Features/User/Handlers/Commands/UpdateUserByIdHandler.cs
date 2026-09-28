using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class UpdateUserByIdHandler(
    IUserRepository userRepository,
    IRepository<Domain.Entities.Role> roleRepository,
    IUserRegionService userRegionService,
    IUserContextService? userContextService = null,
    IUserAuthorizationService? userAuthorizationService = null) : IRequestHandler<UpdateUserByIdRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IUserRegionService _userRegionService = userRegionService;
    private readonly IUserContextService? _userContextService = userContextService;
    private readonly IUserAuthorizationService? _userAuthorizationService = userAuthorizationService;

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

        // Prevent users from changing their own role
        var callerAuthId = _userContextService?.GetCurrentUserId();
        if (!string.IsNullOrEmpty(callerAuthId) && string.Equals(user.AuthUserId, callerAuthId, StringComparison.OrdinalIgnoreCase))
        {
            if (request.User.RoleId.HasValue && request.User.RoleId.Value != user.RoleId)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "You cannot modify your own role.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "RoleId", ["Self-modification of user role is forbidden."] }
                    }
                };
            }
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

            // Role assignment jurisdiction check
            if (_userAuthorizationService != null)
            {
                var isSuperAdmin = await _userAuthorizationService.HasRoleAsync(Roles.SuperAdmin, cancellationToken);
                if (!isSuperAdmin)
                {
                    if (Roles.IsPlatformRole(role.Key) || string.Equals(role.Scope, Roles.ScopePlatform, StringComparison.OrdinalIgnoreCase))
                    {
                        return new ApiResponse<UserDto>
                        {
                            Succeeded = false,
                            Message = "Only Super Administrators can assign Platform-scoped roles."
                        };
                    }

                    if (role.RegionId.HasValue)
                    {
                        var hasAccess = await _userAuthorizationService.HasRegionAccessAsync(role.RegionId.Value, cancellationToken);
                        if (!hasAccess)
                        {
                            return new ApiResponse<UserDto>
                            {
                                Succeeded = false,
                                Message = "You do not have administrative access to assign roles from this region."
                            };
                        }
                    }
                }
            }

            user.RoleId = role.Id;
        }

        if (!string.IsNullOrEmpty(request.User.AuthUserId)) user.AuthUserId = request.User.AuthUserId;

        var existingUserQuery = _userRepository.Query(u => u.Id != request.Id &&
                                 u.RoleId == user.RoleId &&
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
                        "Duplicate", ["User with the same Role and AuthUserId already exists."]
                    }
                }
            };
        }
        user.Email = request.User.Email ?? user.Email;
        user.FirstName = request.User.FirstName ?? user.FirstName;
        user.LastName = request.User.LastName ?? user.LastName;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Auto-assign active regions if user is updated to SuperAdmin
        if (role != null && Roles.IsSuperAdmin(role))
        {
            await _userRegionService.AssignAllRegionsToUserAsync(user.Id, user.RoleId, cancellationToken);
        }

        var result = new UserDto
        {
            Id = user.Id,
            Role = new()
            {
                Id = user.RoleId,
                Name = role?.Name ?? user.Role?.Name ?? string.Empty,
                Key = role?.Key ?? user.Role?.Key ?? string.Empty,
                Description = role?.Description ?? user.Role?.Description ?? string.Empty,
                IsSystemRole = role?.IsSystemRole ?? user.Role?.IsSystemRole ?? false,
                Scope = role?.Scope ?? user.Role?.Scope ?? Roles.ScopeRegional,
                RegionId = role?.RegionId ?? user.Role?.RegionId,
                Permissions = role?.Permissions ?? user.Role?.Permissions ?? []
            },
            AuthUserId = user.AuthUserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        return new ApiResponse<UserDto>
        {
            Data = result,
            Message = "User updated successfully.",
            Succeeded = true
        };
    }
}
