using MediatR;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Commands;

public class CreateUserRegionHandler(
    IUserRegionRepository userRegionRepository,
    IUserRepository userRepository,
    IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<CreateUserRegionRequest, ApiResponse<UserRegionDto>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<ApiResponse<UserRegionDto>> Handle(CreateUserRegionRequest request, CancellationToken cancellationToken)
    {
        var existingUserRegionQuery = _userRegionRepository.Query(ur => 
            ur.UserId == request.UserRegion.UserId && 
            ur.RegionId == request.UserRegion.RegionId);
        var existingUserRegion = await _userRegionRepository.FirstOrDefaultAsync(existingUserRegionQuery, cancellationToken);
        
        if (existingUserRegion is not null)
        {
            return new ApiResponse<UserRegionDto>("User region already exists for the specified user and region.");
        }

        var userQuery = _userRepository.Query(u => u.Id == request.UserRegion.UserId);
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

        var newUserRegion = new Domain.Aggregates.UserRegion
        {
            UserId = request.UserRegion.UserId,
            RegionId = request.UserRegion.RegionId,
        };

        _userRegionRepository.Add(newUserRegion);
        await _userRegionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserRegionDto>
        {
            Data = new UserRegionDto
            {
                Id = newUserRegion.Id,
                Status = newUserRegion.Status,
                User = new()
                {
                    Id = user.Id,
                    AuthUserId = user.AuthUserId,
                    Email = user.Email,
                    Role = new()
                    {
                        Id = user.RoleId,
                        Name = user.Role.Name,
                        Key = user.Role.Key,
                        Description = user.Role.Description
                    },
                    Organization = new()
                    {
                        Id = user.OrganizationId,
                        Name = user.Organization.Name ,
                        Description = user.Organization.Description
                    },
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
            Message = "User region created successfully."
        };
    }
}
