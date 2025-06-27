using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Queries;

public class GetAllPermissionsHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<GetAllPermissionsRequest, PaginatedResponse<PermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;
    public async Task<PaginatedResponse<PermissionDto>> Handle(GetAllPermissionsRequest request, CancellationToken cancellationToken)
    {
        var permissionsQuery = _repository.Query(p => !p.IsDeleted);

        var permissionDtosQuery = permissionsQuery.Select(static p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Key = p.Key,
            Description = p.Description
        });

        return await _repository.PaginateAsync(permissionDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
