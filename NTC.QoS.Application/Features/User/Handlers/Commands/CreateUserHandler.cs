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
        // Prepare all database queries to run in parallel
        var tasks = new List<Task>();
        var taskResults = new Dictionary<string, Task>();

        // Always check for existing user
        var existingUserTask = _userRepository.FirstOrDefaultAsync(_userRepository.Query(u => u.AuthUserId == request.User.AuthUserId), cancellationToken);
        tasks.Add(existingUserTask);
        taskResults["existingUser"] = existingUserTask;

        // Conditionally fetch defaults only when needed
        Task<Domain.Entities.Role?>? defaultRoleTask = null;
        Task<Domain.Entities.Organization?>? defaultOrgTask = null;

        if (request.User.RoleId == Guid.Empty)
        {
            defaultRoleTask = _roleRepository.FirstOrDefaultAsync(_roleRepository.Query(r => r.Name == "user"), cancellationToken);
            tasks.Add(defaultRoleTask);
            taskResults["defaultRole"] = defaultRoleTask;
        }

        if (request.User.OrganizationId == Guid.Empty)
        {
            defaultOrgTask = _organizationRepository.FirstOrDefaultAsync(_organizationRepository.Query(o => o.Name == "public"), cancellationToken);
            tasks.Add(defaultOrgTask);
            taskResults["defaultOrg"] = defaultOrgTask;
        }

        // Conditionally validate explicit IDs
        Task<Domain.Entities.Role?>? explicitRoleTask = null;
        Task<Domain.Entities.Organization?>? explicitOrgTask = null;

        if (request.User.RoleId != Guid.Empty)
        {
            explicitRoleTask = _roleRepository.FirstOrDefaultAsync(_roleRepository.Query(r => r.Id == request.User.RoleId), cancellationToken);
            tasks.Add(explicitRoleTask);
            taskResults["explicitRole"] = explicitRoleTask;
        }

        if (request.User.OrganizationId != Guid.Empty)
        {
            explicitOrgTask = _organizationRepository.FirstOrDefaultAsync(_organizationRepository.Query(o => o.Id == request.User.OrganizationId), cancellationToken);
            tasks.Add(explicitOrgTask);
            taskResults["explicitOrg"] = explicitOrgTask;
        }

        // Execute all queries in parallel
        await Task.WhenAll(tasks);

        // Process results
        var existingUser = ((Task<Domain.Entities.User?>)taskResults["existingUser"]).Result;
        if (existingUser is not null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "User already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["A user with this AuthUserId already exists."]
                    }
                }
            };
        }

        // Get defaults only if they were fetched
        Domain.Entities.Role? defaultRole = null;
        Domain.Entities.Organization? defaultOrg = null;

        if (defaultRoleTask != null)
        {
            defaultRole = defaultRoleTask.Result;
            if (defaultRole == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Default user role not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Role", ["Default 'user' role not found in database"] }
                    }
                };
            }
        }

        if (defaultOrgTask != null)
        {
            defaultOrg = defaultOrgTask.Result;
            if (defaultOrg == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Default public organization not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Organization", ["Default 'public' organization not found in database"] }
                    }
                };
            }
        }

        // Check explicit validations
        if (explicitRoleTask != null)
        {
            var explicitRole = explicitRoleTask.Result;
            if (explicitRole == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Role not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "RoleId", ["Role with the specified Id does not exist"] }
                    }
                };
            }
        }

        if (explicitOrgTask != null)
        {
            var explicitOrg = explicitOrgTask.Result;
            if (explicitOrg == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Organization not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "OrganizationId", ["Organization with the specified Id does not exist."] }
                    }
                };
            }
        }

        // Assign IDs - use defaults only if they were fetched
        Guid roleId = request.User.RoleId != Guid.Empty ? request.User.RoleId : (defaultRole?.Id ?? Guid.Empty);
        Guid organizationId = request.User.OrganizationId != Guid.Empty ? request.User.OrganizationId : (defaultOrg?.Id ?? Guid.Empty);

        var newUser = new Domain.Entities.User
        {
            RoleId = roleId,
            OrganizationId = organizationId,
            AuthUserId = request.User.AuthUserId,
            Email = request.User.Email,
            FirstName = request.User.FirstName,
            LastName = request.User.LastName
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = newUser.Id,
                Role = new()
                {
                    Id = newUser.Role?.Id ?? Guid.Empty,
                    Name = newUser.Role?.Name ?? string.Empty,
                    Key = newUser.Role?.Key ?? string.Empty,
                    Description = newUser.Role?.Description ?? string.Empty
                },
                Organization = new()
                {
                    Id = newUser.Organization?.Id ?? Guid.Empty,
                    Name = newUser.Organization?.Name ?? string.Empty,
                    Description = newUser.Organization?.Description ?? string.Empty
                },
                AuthUserId = newUser.AuthUserId,
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName
            },
            Message = "User created successfully.",
            Succeeded = true
        };
    }
}
