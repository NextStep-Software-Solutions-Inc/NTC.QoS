using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Queries;

public class GetAllPermissionsHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<GetAllPermissionsRequest, PaginatedResponse<GetPermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;
    public async Task<PaginatedResponse<GetPermissionDto>> Handle(GetAllPermissionsRequest request, CancellationToken cancellationToken)
    {
        var allPermissions = await _repository.ListAsync(p => !p.IsDeleted, cancellationToken);

        var permissionDtos = allPermissions.Select(p => new GetPermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Key = p.Key,
            Description = p.Description
        }).ToList();

        var paginatedResponse = new PaginatedResponse<GetPermissionDto>(permissionDtos, request.PageNumber, request.PageSize)
        {
            Message = "Permissions retrieved successfully."
        };

        return paginatedResponse; 
    }
}
