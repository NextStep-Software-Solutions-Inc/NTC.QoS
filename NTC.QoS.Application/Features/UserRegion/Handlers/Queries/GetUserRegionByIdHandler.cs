using MediatR;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Queries;

public class GetUserRegionByIdHandler(IUserRegionRepository userRegionRepository) : IRequestHandler<GetUserRegionByIdRequest, ApiResponse<UserRegionDto>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;

    public async Task<ApiResponse<UserRegionDto>> Handle(GetUserRegionByIdRequest request, CancellationToken cancellationToken)
    {
        var userRegionQuery = _userRegionRepository.Query(ur => ur.Id == request.Id);
        var userRegion = await _userRegionRepository.FirstOrDefaultAsync(userRegionQuery, cancellationToken);

        if (userRegion is null)
        {
            return new ApiResponse<UserRegionDto>("User region with the specified ID does not exist.");
        }

        return new ApiResponse<UserRegionDto>
        {
            Succeeded = true,
            Message = "User region relationship retrieved successfully",
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
                    FirstName = userRegion.User.FirstName,
                    LastName = userRegion.User.LastName,
                    Role = userRegion.User.Role != null ? new()
                    {
                        Id = userRegion.User.Role.Id,
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
