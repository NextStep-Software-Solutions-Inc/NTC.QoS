using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetUserByAuthUserIdHandler(IUserRepository repository) : IRequestHandler<GetUserByAuthUserIdRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _repository = repository;

    public async Task<ApiResponse<UserDto>> Handle(GetUserByAuthUserIdRequest request, CancellationToken cancellationToken)
    {
        var userQuery = _repository.Query(u => u.AuthUserId.Equals(request.AuthUserId));
        var user = await _repository.FirstOrDefaultAsync(userQuery, cancellationToken);
        if (user is null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "AuthUserId", ["User with the specified AuthUserId does not exist."]
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
                Role = new ()
                {
                    Id = user.RoleId,
                    Name = user.Role?.Name ?? string.Empty,
                    Key = user.Role?.Key ?? string.Empty,
                    Description = user.Role?.Description ?? string.Empty
                },
                Organization = new ()
                {
                    Id = user.OrganizationId,
                    Name = user.Organization?.Name ?? string.Empty,
                    Description = user.Organization?.Description ?? string.Empty
                },
                AuthUserId = user.AuthUserId
            },
        };
    }
}
