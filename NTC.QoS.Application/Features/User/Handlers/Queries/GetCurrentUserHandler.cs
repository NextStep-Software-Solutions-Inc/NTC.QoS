using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetCurrentUserHandler(
    IUserContextService userContextService,
    IUserRepository userRepository,
    IUserAuthorizationService userAuthorizationService) : IRequestHandler<GetCurrentUserRequest, ApiResponse<CurrentUserDto>>
{
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<CurrentUserDto>> Handle(GetCurrentUserRequest request, CancellationToken cancellationToken)
    {
        var authUserId = _userContextService.GetCurrentUserId();
        if (string.IsNullOrEmpty(authUserId))
        {
            return new ApiResponse<CurrentUserDto>
            {
                Succeeded = false,
                Message = "User is not authenticated."
            };
        }

        var userQuery = _userRepository.Query(u => u.AuthUserId == authUserId && !u.IsDeleted)
            .Include(u => u.Role);

        var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);
        if (user == null)
        {
            var provisionedUser = await _userAuthorizationService.GetCurrentUserAsync(cancellationToken);
            if (provisionedUser != null)
            {
                user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);
            }
        }

        if (user == null)
        {
            return new ApiResponse<CurrentUserDto>
            {
                Succeeded = false,
                Message = "User record not found in system."
            };
        }

        var permissions = await _userAuthorizationService.GetUserPermissionsAsync(cancellationToken);
        var accessibleRegions = await _userAuthorizationService.GetAccessibleRegionsAsync(cancellationToken);
        var roleKey = user.Role?.Key;
        var isSuperAdmin = Roles.IsSuperAdmin(roleKey);
        var isPlatformUser = Roles.IsPlatformRole(roleKey);

        var currentUserDto = new CurrentUserDto
        {
            Id = user.Id,
            AuthUserId = user.AuthUserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role != null ? new()
            {
                Id = user.Role.Id,
                Name = user.Role.Name,
                Key = user.Role.Key,
                Description = user.Role.Description,
                IsSystemRole = user.Role.IsSystemRole,
                Scope = user.Role.Scope,
                RegionId = user.Role.RegionId,
                Permissions = user.Role.Permissions
            } : null,
            Permissions = permissions,
            AccessibleRegionIds = accessibleRegions,
            IsSuperAdmin = isSuperAdmin,
            IsPlatformUser = isPlatformUser
        };

        return new ApiResponse<CurrentUserDto>
        {
            Succeeded = true,
            Data = currentUserDto,
            Message = "Current user profile retrieved successfully."
        };
    }
}
