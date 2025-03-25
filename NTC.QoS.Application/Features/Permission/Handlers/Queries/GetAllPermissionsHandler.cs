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
        var allPermissions = await _repository.ListAsync(p => !p.IsDeleted, cancellationToken);

        var permissionDtos = allPermissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Key = p.Key,
            Description = p.Description
        }).ToList();

        if (permissionDtos.Count is 0)
        {
            return new PaginatedResponse<PermissionDto>(permissionDtos, request.PageNumber, request.PageSize)
            {
                Data = permissionDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        var paginatedResponse = new PaginatedResponse<PermissionDto>(permissionDtos, request.PageNumber, request.PageSize)
        {
            Data = permissionDtos,
            Message = "Permissions retrieved successfully.",
            Succeeded = true
        };

        return paginatedResponse; 
    }
}
