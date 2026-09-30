using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetAllUsersHandler(
    IUserRepository repository,
    IUserAuthorizationService authorizationService) : IRequestHandler<GetAllUsersRequest, PaginatedResponse<UserDto>>
{
    private readonly IUserRepository _repository = repository;
    private readonly IUserAuthorizationService _authorizationService = authorizationService;

    public async Task<PaginatedResponse<UserDto>> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        // Scope check: If caller is not a Super Admin or Platform officer, enforce their accessible regions
        var currentUser = await _authorizationService.GetCurrentUserAsync(cancellationToken);
        var isPlatformUser = currentUser?.Role != null && Roles.IsPlatformRole(currentUser.Role.Key);

        if (!isPlatformUser)
        {
            var accessibleRegions = await _authorizationService.GetAccessibleRegionsAsync(cancellationToken);
            if (request.RegionIds is not null && request.RegionIds.Count > 0)
            {
                var filteredRegionIds = request.RegionIds.Where(accessibleRegions.Contains).ToList();
                query = query.Where(u => u.UserRegions.Any(ur => filteredRegionIds.Contains(ur.RegionId)));
            }
            else
            {
                query = query.Where(u => u.UserRegions.Any(ur => accessibleRegions.Contains(ur.RegionId)));
            }

            // Lower-level administrators and regional staff must never see SuperAdmin accounts
            query = query.Where(u => u.Role == null || (u.Role.Key != Roles.SuperAdmin && u.Role.Key != "super-admin"));
        }
        else if (request.RegionIds is not null && request.RegionIds.Count > 0)
        {
            query = query.Where(u => u.UserRegions.Any(ur => request.RegionIds.Contains(ur.RegionId)));

            // Regional personnel directory should only display operational regional personnel, not platform SuperAdmins
            query = query.Where(u => u.Role == null || (u.Role.Key != Roles.SuperAdmin && u.Role.Key != "super-admin"));
        }

        if (request.AuthUserIds is not null && request.AuthUserIds.Count > 0)
        {
            query = query.Where(u => request.AuthUserIds.Contains(u.AuthUserId));
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            query = _repository.ApplySearch(query, request.Search, u => u.FirstName, u => u.LastName, u => u.Email);
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            query = query.Where(u => u.Email.ToLower() == request.Email.ToLower());
        }

        var userDtosQuery = query
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Role = u.Role != null ? new()
                {
                    Id = u.RoleId,
                    Name = u.Role.Name,
                    Key = u.Role.Key,
                    Description = u.Role.Description ?? string.Empty,
                    IsSystemRole = u.Role.IsSystemRole,
                    Scope = u.Role.Scope,
                    RegionId = u.Role.RegionId,
                    Permissions = u.Role.Permissions
                } : null,
                AuthUserId = u.AuthUserId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
            });

        return await _repository.PaginateAsync(userDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
