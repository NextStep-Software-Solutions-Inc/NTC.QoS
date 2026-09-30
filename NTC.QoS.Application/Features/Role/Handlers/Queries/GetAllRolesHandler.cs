using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Queries;

public class GetAllRolesHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<GetAllRolesRequest, PaginatedResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;

    public async Task<PaginatedResponse<RoleDto>> Handle(GetAllRolesRequest request, CancellationToken cancellationToken)
    {
        var allRoles = _repository.Query();

        if (!string.IsNullOrEmpty(request.Search))
        {
            allRoles = _repository.ApplySearch(allRoles, request.Search, r => r.Name, r => r.Key);
        }

        if (!string.IsNullOrEmpty(request.Scope))
        {
            allRoles = allRoles.Where(r => r.Scope == request.Scope);
        }

        if (request.RegionId.HasValue)
        {
            allRoles = allRoles.Where(r => r.RegionId == null || r.RegionId == request.RegionId.Value);
        }

        var roleDtos = allRoles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Key = r.Key,
            Description = r.Description ?? string.Empty,
            IsSystemRole = r.IsSystemRole,
            Scope = r.Scope,
            RegionId = r.RegionId,
            Permissions = r.Permissions
        });

        return await _repository.PaginateAsync(roleDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
