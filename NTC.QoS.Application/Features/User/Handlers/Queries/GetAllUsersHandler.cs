using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetAllUsersHandler(IUserRepository repository) : IRequestHandler<GetAllUsersRequest, PaginatedResponse<GetUserDto>>
{
    private readonly IUserRepository _repository = repository;
    public async Task<PaginatedResponse<GetUserDto>> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var allUsers = await _repository.ListAsync(u => !u.IsDeleted, cancellationToken);

        if (request.RoleId.HasValue)
        {
            allUsers = [.. allUsers.Where(u => u.RoleId == request.RoleId.Value)];
        }

        if (request.OrganizationId.HasValue)
        {
            allUsers = [.. allUsers.Where(u => u.OrganizationId == request.OrganizationId.Value)];
        }

        var userDtos = allUsers.Select(u => new GetUserDto
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

        return new PaginatedResponse<GetUserDto>(userDtos, request.PageNumber, request.PageSize)
        {
            Data = userDtos,
            Message = "Users retrieved successfully.",
            Succeeded = true
        };
    }
}