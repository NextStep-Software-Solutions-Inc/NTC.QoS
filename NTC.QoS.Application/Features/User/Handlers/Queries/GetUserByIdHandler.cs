using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetUserByIdHandler(IUserRepository repository) : IRequestHandler<GetUserByIdRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _repository = repository;

    public async Task<ApiResponse<UserDto>> Handle(GetUserByIdRequest request, CancellationToken cancellationToken)
    {
        var userQuery = _repository.Query(u => u.Id.Equals(request.Id))
            .Include(u => u.Role);
        var user = await _repository.FirstOrDefaultAsync(userQuery, cancellationToken);
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

        return new ApiResponse<UserDto>
        {
            Succeeded = true,
            Message = "User retrieved successfully.",
            Data = new UserDto
            {
                Id = user.Id,
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
                AuthUserId = user.AuthUserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            },
        };
    }
}
