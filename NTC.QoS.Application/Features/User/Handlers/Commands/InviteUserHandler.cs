using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class InviteUserHandler(IUserRepository userRepository,
                               IRepository<Domain.Entities.Role> roleRepository,
                               IRepository<Domain.Entities.Organization> organizationRepository) : IRequestHandler<InviteUserRequest, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;

    public async Task<ApiResponse<UserDto>> Handle(InviteUserRequest request, CancellationToken cancellationToken)
    {
        var existingUserQuery = _userRepository.Query(u => u.Email == request.User.Email
                                                           && u.RoleId == request.User.RoleId
                                                           && u.OrganizationId == request.User.OrganizationId);
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
                        "Duplicate", ["User with the same Email, Role, and Organization already exists."] 
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
                        "NullRole", ["Role with the specified Id does not exist"] 
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
                        "NullOrganization", ["Organization with the specified Id does not exist."] 
                    }
                }
            };
        }

        var newUser = new Domain.Entities.User
        {
            Email = request.User.Email,
            RoleId = request.User.RoleId,
            OrganizationId = request.User.OrganizationId,
            AuthUserId = string.Empty 
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
                    Id = role.Id,
                    Name = role.Name,
                    Key = role.Key,
                    Description = role.Description
                },
                Organization = new ()
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    Description = organization.Description
                },
                AuthUserId = newUser.AuthUserId,
                Email = newUser.Email
            },
            Message = "User invitation created successfully.",
            Succeeded = true
        };
    }
}