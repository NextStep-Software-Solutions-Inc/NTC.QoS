using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class UpdateUserByIdHandler(IUserRepository userRepository,
                                   IRepository<Domain.Entities.Role> roleRepository,
                                   IRepository<Domain.Entities.Organization> organizationRepository) : IRequestHandler<UpdateUserByIdRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;

    public async Task<ApiResponse<UserDto>> Handle(UpdateUserByIdRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
        {
            return new ApiResponse<UserDto>
            {
                Data = null,
                Succeeded = false,
                Error = [$"User with ID [{request.Id}] not found."],
                Message = "User does not exist."
            };
        }

        if (request.User.RoleId.HasValue)
        {
            var role = await _roleRepository.GetByIdAsync(request.User.RoleId.Value, cancellationToken);
            if (role is null)
            {
                return new ApiResponse<UserDto>
                {
                    Message = "Role not found.",
                    Succeeded = false,
                    Error = [$"Role with Id [{request.User.RoleId}] does not exist."]
                };
            }
            user.RoleId = role.Id;
        }

        if (request.User.OrganizationId.HasValue)
        {
            var organization = await _organizationRepository.GetByIdAsync(request.User.OrganizationId.Value, cancellationToken);
            if (organization is null)
            {
                return new ApiResponse<UserDto>
                {
                    Message = "Organization not found.",
                    Succeeded = false,
                    Error = [$"Organization with Id [{request.User.OrganizationId}] does not exist."]
                };
            }
            user.OrganizationId = organization.Id;
        }

        user.AuthUserId = request.User.AuthUserId ?? string.Empty;

        var existingUser = await _userRepository.GetAllUsersAsync(cancellationToken);
        if (existingUser.Any(u => u.Id != request.Id &&
                                 u.RoleId == user.RoleId &&
                                 u.OrganizationId == user.OrganizationId &&
                                 u.AuthUserId == user.AuthUserId))
        {
            return new ApiResponse<UserDto>
            {
                Data = null,
                Succeeded = false,
                Error = ["User with the same Role, Organization, and AuthUserId already exists."],
                Message = "Duplicate user data."
            };
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var result = new UserDto
        {
            Id = user.Id,
            Role = new RoleUserDto
            {
                Id = user.RoleId,
                Name = user.Role?.Name ?? string.Empty,
                Key = user.Role?.Key ?? string.Empty,
                Description = user.Role?.Description ?? string.Empty
            },
            Organization = new OrganizationUserDto
            {
                Id = user.OrganizationId,
                Name = user.Organization?.Name ?? string.Empty,
                Description = user.Organization?.Description ?? string.Empty
            },
            AuthUserId = user.AuthUserId
        };

        return new ApiResponse<UserDto>
        {
            Data = result,
            Message = "User updated successfully.",
            Succeeded = true
        };
    }
}
