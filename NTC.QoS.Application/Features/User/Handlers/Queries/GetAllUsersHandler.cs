using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.User;
using Microsoft.EntityFrameworkCore;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetAllUsersHandler(IUserRepository repository, IUserRegionRepository userRegionRepository) : IRequestHandler<GetAllUsersRequest, PaginatedResponse<UserDto>>
{
    private readonly IUserRepository _repository = repository;
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    
    public async Task<PaginatedResponse<UserDto>> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var allUsers = _repository.Query();

        // Apply RegionIds filter through UserRegion relationship
        if (request.RegionIds is not null && request.RegionIds.Count > 0)
        {
            var userIdsInRegions = await _userRegionRepository.Query()
                .Where(ur => request.RegionIds.Contains(ur.RegionId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            allUsers = allUsers.Where(u => userIdsInRegions.Contains(u.Id));
        }

        var userDtos = allUsers.Select(u => new UserDto
        {
            Id = u.Id,
            Role = new ()
            {
                Id = u.RoleId,
                Name = u.Role != null ? u.Role.Name : string.Empty,
                Key = u.Role != null ? u.Role.Key : string.Empty,
                Description = u.Role != null ? u.Role.Description ?? string.Empty : string.Empty
            },
            Organization = new ()
            {
                Id = u.OrganizationId,
                Name = u.Organization != null ? u.Organization.Name : string.Empty,
                Description = u.Organization != null ? u.Organization.Description ?? string.Empty : string.Empty
            },
            AuthUserId = u.AuthUserId
        });

        userDtos = userDtos
            .Where(u => !request.RoleId.HasValue || (u.Role != null && u.Role.Id == request.RoleId))
            .Where(u => !request.OrganizationId.HasValue || (u.Organization != null && u.Organization.Id == request.OrganizationId.Value));

        return await _repository.PaginateAsync(userDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
