using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Commands;

public class CreateUserRegionHandler(
    IUserRegionRepository userRegionRepository,
    IUserRepository userRepository,
    IRepository<Domain.Entities.Region> regionRepository,
    IUserContextService userContextService,
    IUserAuthorizationService userAuthorizationService) : IRequestHandler<CreateUserRegionRequest, ApiResponse<UserRegionDto>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<UserRegionDto>> Handle(CreateUserRegionRequest request, CancellationToken cancellationToken)
    {
        var userQuery = _userRepository.Query(u => u.Id == request.UserRegion.UserId)
            .Include(u => u.Role);
        var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);

        if (user is null)
        {
            return new ApiResponse<UserRegionDto>("User not found.");
        }

        var regionQuery = _regionRepository.Query(r => r.Id == request.UserRegion.RegionId);
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region is null)
        {
            return new ApiResponse<UserRegionDto>("Region not found.");
        }

        var callerAuthId = _userContextService.GetCurrentUserId();
        var isSuperAdmin = await _userAuthorizationService.HasRoleAsync(Roles.SuperAdmin, cancellationToken);
        var isSelfRequest = string.Equals(user.AuthUserId, callerAuthId, StringComparison.OrdinalIgnoreCase);

        // Regular users requesting region access are always Pending
        // Super admin assigning to someone else can immediately activate
        var initialStatus = (!isSelfRequest && isSuperAdmin)
            ? UserRegionStatus.Active.ToString()
            : UserRegionStatus.Pending.ToString();

        var existingUserRegionQuery = _userRegionRepository.Query(ur =>
            ur.UserId == request.UserRegion.UserId &&
            ur.RegionId == request.UserRegion.RegionId);
        var existingUserRegion = await _userRegionRepository.FirstOrDefaultAsync(existingUserRegionQuery, cancellationToken);

        if (existingUserRegion is not null)
        {
            if (string.Equals(existingUserRegion.Status, UserRegionStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<UserRegionDto>("User already has active access to this region.");
            }

            if (string.Equals(existingUserRegion.Status, UserRegionStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<UserRegionDto>("An access request for this region is already pending approval.");
            }

            // If previously inactive or revoked, set to initial status (Pending unless admin-assigned)
            existingUserRegion.Status = initialStatus;
            if (request.UserRegion.RoleId.HasValue)
            {
                existingUserRegion.RoleId = request.UserRegion.RoleId.Value;
            }
            _userRegionRepository.Update(existingUserRegion);
            await _userRegionRepository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<UserRegionDto>
            {
                Data = new UserRegionDto
                {
                    Id = existingUserRegion.Id,
                    Status = existingUserRegion.Status,
                    RoleId = existingUserRegion.RoleId,
                    User = new()
                    {
                        Id = user.Id,
                        AuthUserId = user.AuthUserId,
                        Email = user.Email,
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
                    },
                    Region = new()
                    {
                        Id = region.Id,
                        Name = region.Name,
                        Code = region.Code,
                        Description = region.Description
                    }
                },
                Succeeded = true,
                Message = initialStatus == UserRegionStatus.Active.ToString()
                    ? "User region assigned successfully."
                    : "User region access request submitted for approval."
            };
        }

        var newUserRegion = new Domain.Aggregates.UserRegion
        {
            UserId = request.UserRegion.UserId,
            RegionId = request.UserRegion.RegionId,
            RoleId = request.UserRegion.RoleId,
            Status = initialStatus
        };

        _userRegionRepository.Add(newUserRegion);
        await _userRegionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserRegionDto>
        {
            Data = new UserRegionDto
            {
                Id = newUserRegion.Id,
                Status = newUserRegion.Status,
                RoleId = newUserRegion.RoleId,
                User = new()
                {
                    Id = user.Id,
                    AuthUserId = user.AuthUserId,
                    Email = user.Email,
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
                },
                Region = new()
                {
                    Id = region.Id,
                    Name = region.Name,
                    Code = region.Code,
                    Description = region.Description
                }
            },
            Succeeded = true,
            Message = initialStatus == UserRegionStatus.Active.ToString()
                ? "User region assigned successfully."
                : "User region access request submitted for approval."
        };
    }
}
