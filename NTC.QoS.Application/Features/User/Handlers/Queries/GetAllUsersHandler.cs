using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetAllUsersHandler(IUserRepository repository) : IRequestHandler<GetAllUsersRequest, PaginatedResponse<UserDto>>
{
    private readonly IUserRepository _repository = repository;
    public async Task<PaginatedResponse<UserDto>> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var allUsers = _repository.Query(u => !u.IsDeleted);

        var userDtos = allUsers.Select(u => new UserDto
        {
            Id = u.Id,
            Role = new RoleUserDto
            {
                Id = u.RoleId,
                Name = u.Role != null ? u.Role.Name : string.Empty,
                Key = u.Role != null ? u.Role.Key : string.Empty,
                Description = u.Role != null ? u.Role.Description ?? string.Empty : string.Empty
            },
            Organization = new OrganizationUserDto
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
