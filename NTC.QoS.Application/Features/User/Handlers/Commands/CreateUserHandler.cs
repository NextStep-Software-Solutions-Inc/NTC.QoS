using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class CreateUserHandler(
    IRepository<Domain.Entities.User> userRepository,
    IRepository<Domain.Entities.Role> roleRepository,
    IUserRegionService userRegionService,
    IUserRegionRepository userRegionRepository) : IRequestHandler<CreateUserRequest, ApiResponse<UserDto>>
{
    private readonly IRepository<Domain.Entities.User> _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IUserRegionService _userRegionService = userRegionService;
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;

    public async Task<ApiResponse<UserDto>> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Step 1: Check if user already exists
        var existingUser = await _userRepository.FirstOrDefaultAsync(_userRepository.Query(u => u.AuthUserId == request.User.AuthUserId), cancellationToken);

        if (existingUser != null)
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "User already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["A user with this AuthUserId already exists."] }
                }
            };
        }

        // Step 2: Get default role if not provided
        Domain.Entities.Role? defaultRole = null;
        if (request.User.RoleId == Guid.Empty)
        {
            defaultRole = await _roleRepository.FirstOrDefaultAsync(_roleRepository.Query(r => r.Key == "user"), cancellationToken);

            if (defaultRole == null)
            {
                return new ApiResponse<UserDto>
                {
                    Succeeded = false,
                    Message = "Default user role key not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Role", ["Default 'user' role key not found in database"] }
                    }
                };
            }
        }

        // Step 3: Validate explicit role if provided
        Domain.Entities.Role? providedRole = null;
        if (request.User.RoleId != Guid.Empty)
        {
            providedRole = await _roleRepository.FirstOrDefaultAsync(_roleRepository.Query(r => r.Id == request.User.RoleId), cancellationToken);

            if (providedRole == null)
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

        Guid roleId = request.User.RoleId == Guid.Empty ? defaultRole!.Id : request.User.RoleId;

        // Step 4: Create the user
        var newUser = new Domain.Entities.User
        {
            RoleId = roleId,
            AuthUserId = request.User.AuthUserId,
            Email = request.User.Email,
            FirstName = request.User.FirstName,
            LastName = request.User.LastName
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Step 5: Auto-assign active regions if user has SuperAdmin role, or assign specified regions
        var effectiveRole = providedRole ?? defaultRole;
        if (Roles.IsSuperAdmin(effectiveRole))
        {
            await _userRegionService.AssignAllRegionsToUserAsync(newUser.Id, newUser.RoleId, cancellationToken);
        }
        else if (request.User.Regions is { Count: > 0 })
        {
            var userRegions = request.User.Regions
                .GroupBy(r => r.RegionId)
                .Select(g => g.First())
                .Select(r => new Domain.Aggregates.UserRegion
                {
                    Id = Guid.NewGuid(),
                    UserId = newUser.Id,
                    RegionId = r.RegionId,
                    RoleId = r.RoleId ?? newUser.RoleId,
                    Status = string.IsNullOrWhiteSpace(r.Status) ? UserRegionStatus.Active.ToString() : r.Status
                }).ToList();

            _userRegionRepository.AddRange(userRegions);
            await _userRegionRepository.SaveChangesAsync(cancellationToken);
        }

        // Step 6: Return success response
        return new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = newUser.Id,
                Role = new()
                {
                    Id = effectiveRole?.Id ?? newUser.RoleId,
                    Name = effectiveRole?.Name ?? string.Empty,
                    Key = effectiveRole?.Key ?? string.Empty,
                    Description = effectiveRole?.Description ?? string.Empty
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

