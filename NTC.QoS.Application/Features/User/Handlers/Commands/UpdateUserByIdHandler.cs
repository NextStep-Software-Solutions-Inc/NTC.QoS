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
        var userQuery = _userRepository.Query(r => r.Id == request.Id);
        var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);
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

        if (request.User.RoleId.HasValue)
        {
            var roleQuery = _roleRepository.Query(r => r.Id == request.User.RoleId.Value);
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
                            "RoleId", ["Role with the specified ID does not exist."] 
                        }
                    }
                };
            }
            user.RoleId = role.Id;
        }

        if (request.User.OrganizationId.HasValue)
        {
            var organizationQuery = _organizationRepository.Query(r => r.Id == request.User.OrganizationId.Value);
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
                            "OrganizationId", ["Organization with the specified ID does not exist."] 
                        }
                    }
                };
            }
            user.OrganizationId = organization.Id;
        }

        if (!string.IsNullOrEmpty(request.User.AuthUserId)) user.AuthUserId = request.User.AuthUserId;

        var existingUserQuery = _userRepository.Query(u => u.Id != request.Id &&
                                 u.RoleId == user.RoleId &&
                                 u.OrganizationId == user.OrganizationId &&
                                 u.AuthUserId == user.AuthUserId);
        var existingUser = await _userRepository.FirstOrDefaultAsync(existingUserQuery, cancellationToken);
        if (existingUser != null)
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

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var result = new UserDto
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
            AuthUserId = user.AuthUserId,
            Email = user.Email,
        };

        return new ApiResponse<UserDto>
        {
            Data = result,
            Message = "User updated successfully.",
            Succeeded = true
        };
    }
}
