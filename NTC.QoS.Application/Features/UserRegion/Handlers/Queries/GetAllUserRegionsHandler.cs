using MediatR;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.UserRegion;
using Microsoft.EntityFrameworkCore;
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

        var totalCount = await query.CountAsync(cancellationToken);
        var userRegions = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ur => new UserRegionDto
            {
                Id = ur.Id,
                UserId = ur.UserId,
                RegionId = ur.RegionId,
                Status = ur.Status,
                User = ur.User != null ? new()
                {
                    Id = ur.User.Id,
                    AuthUserId = ur.User.AuthUserId,
                    Role = ur.User.Role != null ? new()
                    {
                        Id = ur.User.RoleId,
                        Name = ur.User.Role.Name,
                        Key = ur.User.Role.Key,
                        Description = ur.User.Role.Description ?? string.Empty
                    } : null,
                    Organization = ur.User.Organization != null ? new()
                    {
                        Id = ur.User.OrganizationId,
                        Name = ur.User.Organization.Name,
                        Description = ur.User.Organization.Description ?? string.Empty
                    } : null
                } : null,
                Region = ur.Region != null ? new()
                {
                    Id = ur.Region.Id,
                    Name = ur.Region.Name,
                    Code = ur.Region.Code,
                    Description = ur.Region.Description ?? string.Empty
                } : null
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<UserRegionDto>(userRegions, totalCount, request.PageNumber, request.PageSize);
    }
}
