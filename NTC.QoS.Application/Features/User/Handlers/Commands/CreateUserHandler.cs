using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class CreateUserHandler(IRepository<Domain.Entities.User> userRepository,
                                   IRepository<Domain.Entities.Role> roleRepository,
                                   IRepository<Domain.Entities.Organization> organizationRepository) : IRequestHandler<CreateUserRequest, ApiResponse<UserDto>>
{
    private readonly IRepository<Domain.Entities.User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;

    public async Task<ApiResponse<UserDto>> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.FindAsync(u => u.RoleId == request.User.RoleId
                                                           && u.OrganizationId == request.User.OrganizationId
                                                           && u.AuthUserId == request.User.AuthUserId, cancellationToken);

        if (existingUser is not null)
        {
            return new ApiResponse<UserDto>
            {
                Message = "User already exists.",
                Succeeded = false,
                Error = ["User with the same Role, Organization, and AuthUserId already exists."]
            };
        }
        
        var role = await _roleRepository.GetByIdAsync(request.User.RoleId, cancellationToken);
        if (role is null)
        {
            return new ApiResponse<UserDto>
            {
                Message = "Role not found.",
                Succeeded = false,
                Error = [$"Role with Id [{request.User.RoleId}] does not exist."]
            };
        }
        
        var organization = await _organizationRepository.GetByIdAsync(request.User.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return new ApiResponse<UserDto>
            {
                Message = "Organization not found.",
                Succeeded = false,
                Error = [$"Organization with Id [{request.User.OrganizationId}] does not exist."]
            };
        }

        var newUser = new Domain.Entities.User
        {
            RoleId = role.Id,
            OrganizationId = organization.Id,
            AuthUserId = request.User.AuthUserId
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = newUser.Id,
                Role = new RoleUserDto
                {
                    Id = newUser.RoleId,
                    Name = newUser.Role?.Name ?? string.Empty,
                    Key = newUser.Role?.Key ?? string.Empty,
                    Description = newUser.Role?.Description ?? string.Empty
                },
                Organization = new OrganizationUserDto
                {
                    Id = newUser.OrganizationId,
                    Name = newUser.Organization?.Name ?? string.Empty,
                    Description = newUser.Organization?.Description ?? string.Empty
                },
                AuthUserId = newUser.AuthUserId
            },
            Message = "User created successfully.",
            Succeeded = true
        };
    }
}
