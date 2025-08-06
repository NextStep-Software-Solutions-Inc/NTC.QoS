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
            return new ApiResponse<UserRegionDto>
            {
                Succeeded = false,
                Message = "User region not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "UserRegion", ["User region with the specified ID does not exist."] }
                }
            };
        }

        return new ApiResponse<UserRegionDto>
        {
            Succeeded = true,
            Message = "User region relationship retrieved successfully",
            Data = new UserRegionDto
            {
                Id = userRegion.Id,
                UserId = userRegion.UserId,
                RegionId = userRegion.RegionId,
                Status = userRegion.Status,
                User = userRegion.User != null ? new()
                {
                    Id = userRegion.User.Id,
                    AuthUserId = userRegion.User.AuthUserId,
                    Role = userRegion.User.Role != null ? new()
                    {
                        Id = userRegion.User.RoleId,
                        Name = userRegion.User.Role.Name,
                        Key = userRegion.User.Role.Key,
                        Description = userRegion.User.Role.Description ?? string.Empty
                    } : null,
                    Organization = userRegion.User.Organization != null ? new()
                    {
                        Id = userRegion.User.OrganizationId,
                        Name = userRegion.User.Organization.Name,
                        Description = userRegion.User.Organization.Description ?? string.Empty
                    } : null
                } : null,
                Region = userRegion.Region != null ? new()
                {
                    Id = userRegion.Region.Id,
                    Name = userRegion.Region.Name,
                    Code = userRegion.Region.Code,
                    Description = userRegion.Region.Description ?? string.Empty
                } : null
            }
        };
    }
}
