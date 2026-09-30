using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Queries;

public class GetAllPermissionsHandler : IRequestHandler<GetAllPermissionsRequest, PaginatedResponse<PermissionDto>>
{
    public Task<PaginatedResponse<PermissionDto>> Handle(GetAllPermissionsRequest request, CancellationToken cancellationToken)
    {
        var items = PermissionsRegistry.All.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            items = items.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var totalRows = items.Count();
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;

        var pagedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PermissionDto
            {
                Key = p.Key,
                Name = p.Name,
                Category = p.Category,
                Description = p.Description
            })
            .ToList();

        var response = new PaginatedResponse<PermissionDto>(pagedItems, pageNumber, pageSize, totalRows);

        return Task.FromResult(response);
    }
}
