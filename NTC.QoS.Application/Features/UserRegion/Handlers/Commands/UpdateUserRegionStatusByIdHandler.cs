using MediatR;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.UserRegion;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Commands;

public class UpdateUserRegionStatusByIdHandler(IUserRegionRepository userRegionRepository) : IRequestHandler<UpdateUserRegionStatusByIdRequest, ApiResponse<UserRegionDto>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;

    public async Task<ApiResponse<UserRegionDto>> Handle(UpdateUserRegionStatusByIdRequest request, CancellationToken cancellationToken)
    {
        var userRegionQuery = _userRegionRepository.Query(ur => ur.Id == request.Id);
        var userRegion = await _userRegionRepository.FirstOrDefaultAsync(userRegionQuery, cancellationToken);
        
        if (userRegion is null)
        {
            return new ApiResponse<UserRegionDto>
            {
                Succeeded = false,
                Message = "UserRegion not found",
                Errors = new Dictionary<string, string[]>
                {
                    { "UserRegion", ["UserRegion with the specified ID does not exist."] }
                }
            };
        }

        if (!string.IsNullOrEmpty(request.UserRegion.Status))
        {
            var parsedStatus = EnumHelper<UserRegionStatus>.ParseUserInput(request.UserRegion.Status);
            if (parsedStatus != default)
            {
                userRegion.Status = parsedStatus.ToString();
            }
            else
            {
                return new ApiResponse<UserRegionDto>
                {
                    Succeeded = false,
                    Message = "Invalid status value",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Status", ["Invalid status value provided."] }
                    }
                };
            }
        }

        await _userRegionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UserRegionDto>
        {
            Succeeded = true,
            Message = "UserRegion updated successfully",
            Data = new UserRegionDto
            {
                Id = userRegion.Id,
                Status = userRegion.Status.ToString(),
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
