using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Commands;

public class UpdateUserRegionStatusByIdHandler(
    IUserRegionRepository userRegionRepository,
    IUserContextService userContextService,
    IUserAuthorizationService userAuthorizationService) : IRequestHandler<UpdateUserRegionStatusByIdRequest, ApiResponse<UserRegionDto>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<UserRegionDto>> Handle(UpdateUserRegionStatusByIdRequest request, CancellationToken cancellationToken)
    {
        var userRegionQuery = _userRegionRepository.Query(ur => ur.Id == request.Id)
            .Include(ur => ur.User)
                .ThenInclude(u => u.Role)
            .Include(ur => ur.Role)
            .Include(ur => ur.Region);

        var userRegion = await _userRegionRepository.FirstOrDefaultAsync(userRegionQuery, cancellationToken);

        if (userRegion is null)
        {
            return new ApiResponse<UserRegionDto>("User Region with the specified ID does not exist.");
        }

        // Prevent self-approval
        var callerAuthId = _userContextService.GetCurrentUserId();
        if (string.Equals(userRegion.User.AuthUserId, callerAuthId, StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponse<UserRegionDto>
            {
                Succeeded = false,
                Message = "You cannot approve or alter the status of your own region access requests.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Authorization", ["Self-approval of region access requests is forbidden."] }
                }
            };
        }

        // Ensure non-super admin has access to the target region
        var isSuperAdmin = await _userAuthorizationService.HasRoleAsync(Roles.SuperAdmin, cancellationToken);
        if (!isSuperAdmin)
        {
            var hasRegionAccess = await _userAuthorizationService.HasRegionAccessAsync(userRegion.RegionId, cancellationToken);
            if (!hasRegionAccess)
            {
                return new ApiResponse<UserRegionDto>
                {
                    Succeeded = false,
                    Message = "You do not have administrative access to manage region access in this region."
                };
            }
        }

        if (!string.IsNullOrEmpty(request.UserRegion.Status))
        {
            var parsedStatus = EnumHelper<UserRegionStatus>.ParseUserInput(request.UserRegion.Status);
            userRegion.Status = parsedStatus.ToString();
        }

        await _userRegionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserRegionDto>
        {
            Succeeded = true,
            Message = "User Region updated successfully",
            Data = new UserRegionDto
            {
                Id = userRegion.Id,
                Status = userRegion.Status,
                RoleId = userRegion.RoleId,
                Role = userRegion.Role != null ? new()
                {
                    Id = userRegion.Role.Id,
                    Name = userRegion.Role.Name,
                    Key = userRegion.Role.Key,
                    Description = userRegion.Role.Description,
                    IsSystemRole = userRegion.Role.IsSystemRole,
                    Scope = userRegion.Role.Scope,
                    RegionId = userRegion.Role.RegionId,
                    Permissions = userRegion.Role.Permissions
                } : null,
                User = userRegion.User != null ? new()
                {
                    Id = userRegion.User.Id,
                    AuthUserId = userRegion.User.AuthUserId,
                    Email = userRegion.User.Email,
                    Role = userRegion.User.Role != null ? new()
                    {
                        Id = userRegion.User.RoleId,
                        Name = userRegion.User.Role.Name,
                        Key = userRegion.User.Role.Key,
                        Description = userRegion.User.Role.Description,
                        IsSystemRole = userRegion.User.Role.IsSystemRole,
                        Scope = userRegion.User.Role.Scope,
                        RegionId = userRegion.User.Role.RegionId,
                        Permissions = userRegion.User.Role.Permissions
                    } : null
                } : null,
                Region = userRegion.Region != null ? new()
                {
                    Id = userRegion.Region.Id,
                    Name = userRegion.Region.Name,
                    Code = userRegion.Region.Code,
                    Description = userRegion.Region.Description
                } : null
            }
        };
    }
}
