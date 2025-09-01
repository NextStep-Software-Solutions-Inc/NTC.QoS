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
        var existingUserQuery = _userRepository.Query(u => u.RoleId == request.User.RoleId
                                                           && u.OrganizationId == request.User.OrganizationId
                                                           && u.AuthUserId == request.User.AuthUserId);
        var existingUser = await _userRepository.FirstOrDefaultAsync(existingUserQuery, cancellationToken);
        if (existingUser is not null) 
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Duplicate user found.",
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Duplicate", ["User with the same Role, Organization, and AuthUserId already exists."] 
                    }
                }
            };
        }
        
        var roleQuery = _roleRepository.Query(r => r.Id == request.User.RoleId);
        var role = await _roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);

        if (role is null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Role not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "RoleId", ["Role with the specified Id does not exist"] 
                    }
                }
            };
        }

        var organizationQuery = _organizationRepository.Query(r => r.Id == request.User.OrganizationId);
        var organization = await _organizationRepository.FirstOrDefaultAsync(organizationQuery, cancellationToken);
        if (organization is null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Organization not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "OrganizationId", ["Organization with the specified Id does not exist."] 
                    }
                }
            };
        }

        var newUser = new Domain.Entities.User
        {
            RoleId = request.User.RoleId,
            OrganizationId = request.User.OrganizationId,
            AuthUserId = request.User.AuthUserId,
            Email = request.User.Email
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = newUser.Id,
                Role = new ()
                {
                    Id = newUser.Role?.Id ?? Guid.Empty,
                    Name = newUser.Role?.Name ?? string.Empty,
                    Key = newUser.Role?.Key ?? string.Empty,
                    Description = newUser.Role?.Description ?? string.Empty
                },
                Organization = new ()
                {
                    Id = newUser.Organization?.Id ?? Guid.Empty,
                    Name = newUser.Organization?.Name ?? string.Empty,
                    Description = newUser.Organization?.Description ?? string.Empty
                },
                AuthUserId = newUser.AuthUserId,
                Email = newUser.Email
            },
            Message = "User created successfully.",
            Succeeded = true
        };
    }
}
