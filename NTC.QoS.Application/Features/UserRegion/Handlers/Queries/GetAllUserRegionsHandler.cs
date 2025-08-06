using MediatR;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.UserRegion;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.UserRegion.Handlers.Queries;

public class GetAllUserRegionsHandler(IUserRegionRepository userRegionRepository) : IRequestHandler<GetAllUserRegionsRequest, PaginatedResponse<UserRegionDto>>
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    
    public async Task<PaginatedResponse<UserRegionDto>> Handle(GetAllUserRegionsRequest request, CancellationToken cancellationToken)
    {
        var query = _userRegionRepository.Query();

        if (request.UserIds is not null && request.UserIds.Count > 0)
        {
            query = query.Where(ur => request.UserIds.Contains(ur.UserId));
        }

        if (request.RegionIds is not null && request.RegionIds.Count > 0)
        {
            query = query.Where(ur => request.RegionIds.Contains(ur.RegionId));
        }

        if (request.Statuses is not null && request.Statuses.Count > 0)
        {
            var validStatusStrings = new List<string>();
            foreach (var statusString in request.Statuses)
            {
                var parsedStatus = EnumHelper<UserRegionStatus>.ParseUserInput(statusString);
                if (parsedStatus != UserRegionStatus.Unknown)
                {
                    validStatusStrings.Add(parsedStatus.ToString());
                }
            }

            if (validStatusStrings.Count > 0)
            {
                query = query.Where(ur => validStatusStrings.Contains(ur.Status));
            }
        }

        var userRegions = query
        .Select(ur => new UserRegionDto
            {
                Id = ur.Id,
                Status = ur.Status,
                User = new()
                {
                    Id = ur.User.Id,
                    AuthUserId = ur.User.AuthUserId,
                    Role = new()
                    {
                        Id = ur.User.RoleId,
                        Name = ur.User.Role.Name,
                        Key = ur.User.Role.Key,
                        Description = ur.User.Role.Description
                    },
                    Organization = new()
                    {
                        Id = ur.User.OrganizationId,
                        Name = ur.User.Organization.Name,
                        Description = ur.User.Organization.Description
                    }
                },
                Region = new()
                {
                    Id = ur.Region.Id,
                    Name = ur.Region.Name,
                    Code = ur.Region.Code,
                    Description = ur.Region.Description
                }
            });

        return await _userRegionRepository.PaginateAsync(userRegions, pageNumber: request.PageNumber, pageSize: request.PageSize, cancellationToken);
    }
}
