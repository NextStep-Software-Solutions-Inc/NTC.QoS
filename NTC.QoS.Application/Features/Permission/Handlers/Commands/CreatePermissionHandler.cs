using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Commands;

public class CreatePermissionHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<CreatePermissionRequest, ApiResponse<PermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<PermissionDto>> Handle(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var permissionQueryAll = _repository.QueryWithIgnoreQueryFilters(p => p.Name == request.Permission.Name);
        var existing = await _repository.FirstOrDefaultAsync(permissionQueryAll, cancellationToken);

        if (existing is not null && existing.IsDeleted)
        {
            var restoreQuery = _repository.QueryWithIgnoreQueryFilters(p => p.Id == existing.Id && p.IsDeleted);
            await _repository.RestoreAsync(restoreQuery, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return new ApiResponse<PermissionDto>
            {
                Data = new PermissionDto
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    Key = existing.Key,
                    Description = existing.Description,
                },
                Succeeded = true,
                Message = "Permission restored successfully."
            };
        }

        if (existing is not null) return new ApiResponse<PermissionDto>("Permission already exists.");

        var newPermission = new Domain.Entities.Permission
        {
            Name = request.Permission.Name,
            Key = request.Permission.Key,
            Description = request.Permission.Description,
        };

        _repository.Add(newPermission);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<PermissionDto>
        {
            Data = new PermissionDto
            {
                Id = newPermission.Id,
                Name = newPermission.Name,
                Key = newPermission.Key,
                Description = newPermission.Description,
            },
            Succeeded = true,
            Message = "Permission created successfully.",
        };
    }
}
