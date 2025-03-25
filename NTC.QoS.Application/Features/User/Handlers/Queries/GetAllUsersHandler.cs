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
        var allUsers = await _repository.ListAsync(u => !u.IsDeleted, cancellationToken);

        var userDtos = allUsers.Select(u => new UserDto
        {
            Id = u.Id,
            Role = new RoleUserDto
            {
                Id = u.RoleId,
                Name = u.Role?.Name ?? string.Empty,
                Key = u.Role?.Key ?? string.Empty,
                Description = u.Role?.Description ?? string.Empty
            },
            Organization = new OrganizationUserDto
            {
                Id = u.OrganizationId,
                Name = u.Organization?.Name ?? string.Empty,
                Description = u.Organization?.Description ?? string.Empty
            },
            AuthUserId = u.AuthUserId
        }).ToList();

        userDtos = [.. userDtos
            .Where(u => !request.RoleId.HasValue || u.Role?.Id == request.RoleId)
            .Where(u => !request.OrganizationId.HasValue || u.Organization?.Id == request.OrganizationId.Value)];

        if (userDtos.Count is 0)
        {
            return new PaginatedResponse<UserDto>(userDtos, request.PageNumber, request.PageSize)
            {
                Data = userDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<UserDto>(userDtos, request.PageNumber, request.PageSize)
        {
            Data = userDtos,
            Message = "Users retrieved successfully.",
            Succeeded = true
        };
    }
}
