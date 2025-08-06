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
        // Check for duplicate
        var existingUserRegionQuery = _userRegionRepository.Query(ur => 
            ur.UserId == request.UserRegion.UserId && 
            ur.RegionId == request.UserRegion.RegionId);
        var existingUserRegion = await _userRegionRepository.FirstOrDefaultAsync(existingUserRegionQuery, cancellationToken);
        
        if (existingUserRegion is not null)
        {
            return new ApiResponse<UserRegionDto>
            {
                Succeeded = false,
                Message = "User region already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["User is already associated with this region."] }
                }
            };
        }

        // Validate User exists
        var userQuery = _userRepository.Query(u => u.Id == request.UserRegion.UserId);
        var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);
        
        if (user is null)
        {
            return new ApiResponse<UserRegionDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullUser", ["User not found."] }
                },
                Message = "User not found."
            };
        }

        // Validate Region exists
        var regionQuery = _regionRepository.Query(r => r.Id == request.UserRegion.RegionId);
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);
        
        if (region is null)
        {
            return new ApiResponse<UserRegionDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullRegion", ["Region not found."] }
                },
                Message = "Region not found."
            };
        }

        var newUserRegion = new Domain.Aggregates.UserRegion
        {
            UserId = request.UserRegion.UserId,
            RegionId = request.UserRegion.RegionId,
            Status = request.UserRegion.Status
        };

        _userRegionRepository.Add(newUserRegion);
        await _userRegionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserRegionDto>
        {
            Data = new UserRegionDto
            {
                Id = newUserRegion.Id,
                UserId = newUserRegion.UserId,
                RegionId = newUserRegion.RegionId,
                Status = newUserRegion.Status,
                User = new()
                {
                    Id = user.Id,
                    AuthUserId = user.AuthUserId,
                    Role = new()
                    {
                        Id = user.RoleId,
                        Name = user.Role?.Name ?? string.Empty,
                        Key = user.Role?.Key ?? string.Empty,
                        Description = user.Role?.Description ?? string.Empty
                    },
                    Organization = new()
                    {
                        Id = user.OrganizationId,
                        Name = user.Organization?.Name ?? string.Empty,
                        Description = user.Organization?.Description ?? string.Empty
                    }
                },
                Region = new()
                {
                    Id = region.Id,
                    Name = region.Name,
                    Code = region.Code,
                    Description = region.Description ?? string.Empty
                }
            },
            Succeeded = true,
            Message = "User region created successfully."
        };
    }
}
