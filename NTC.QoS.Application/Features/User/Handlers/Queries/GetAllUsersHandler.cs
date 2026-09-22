using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetAllUsersHandler(IUserRepository repository) : IRequestHandler<GetAllUsersRequest, PaginatedResponse<UserDto>>
{
    private readonly IUserRepository _repository = repository;

    public async Task<PaginatedResponse<UserDto>> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        if (request.RoleId.HasValue)
        {
            query = query.Where(u => u.RoleId == request.RoleId.Value);
        }

        if (request.RegionIds is not null && request.RegionIds.Count > 0)
        {
            query = query.Where(u => u.UserRegions.Any(ur => request.RegionIds.Contains(ur.RegionId)));
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
