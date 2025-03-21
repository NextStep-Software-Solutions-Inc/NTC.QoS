using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;
using System.Data;

namespace NTC.QoS.Application.Features.Permission.Handlers.Commands;

public class UpdatePermissionByIdHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<UpdatePermissionByIdRequest, ApiResponse<PermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<PermissionDto>> Handle(UpdatePermissionByIdRequest request,
        CancellationToken cancellationToken)
    {
        var permission = await _repository.FindAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);
        if (permission is null) return new ApiResponse<PermissionDto>("Permission not found.");

        if (!string.IsNullOrEmpty(request.Permission.Name))
        {
            var existingPermissionWithName = await _repository.FindAsync(
            p => p.Name == request.Permission.Name && p.Id != request.Id && !p.IsDeleted,
            cancellationToken);
            if (existingPermissionWithName is not null) return new ApiResponse<PermissionDto>("Permission name already exists.");
            permission.Name = request.Permission.Name;
        }
        
        if (!string.IsNullOrEmpty(request.Permission.Key))
        {
            var existingPermissionWithKey = await _repository.FindAsync(
            p => p.Key == request.Permission.Key && p.Id != request.Id && !p.IsDeleted,
            cancellationToken);
            if (existingPermissionWithKey is not null) return new ApiResponse<PermissionDto>("Permission key already exists.");
            permission.Key = request.Permission.Key;
        }

        if (!string.IsNullOrEmpty(request.Permission.Description)) permission.Description = request.Permission.Description;

        _repository.Update(permission);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<PermissionDto>
        {
            Succeeded = true,
            Data = new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Key = permission.Key,
                Description = permission.Description
            },
            Message = "Permission updated successfully.",
        };
    }
}
