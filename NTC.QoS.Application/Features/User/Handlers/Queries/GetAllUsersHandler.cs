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

        if (request.OrganizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == request.OrganizationId.Value);
        }

        if (request.RegionIds is not null && request.RegionIds.Count > 0)
        {
            query = query.Where(u => u.UserRegions.Any(ur => request.RegionIds.Contains(ur.RegionId)));
        }
        if (request.AuthUserIds is not null && request.AuthUserIds.Count > 0)
        {
            query = query.Where(u => request.AuthUserIds.Contains(u.AuthUserId));
        }

        var userDtosQuery = query
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Role = new()
                {
                    Id = u.RoleId,
                    Name = u.Role != null ? u.Role.Name : string.Empty,
                    Key = u.Role != null ? u.Role.Key : string.Empty,
                    Description = u.Role != null ? u.Role.Description ?? string.Empty : string.Empty
                },
                Organization = new()
                {
                    Id = u.OrganizationId,
                    Name = u.Organization != null ? u.Organization.Name : string.Empty,
                    Description = u.Organization != null ? u.Organization.Description ?? string.Empty : string.Empty
                },
                AuthUserId = u.AuthUserId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
            });

        return await _repository.PaginateAsync(userDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
