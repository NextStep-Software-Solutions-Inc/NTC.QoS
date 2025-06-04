using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Commands;

public class CreatePermissionHandler(IRepository<Domain.Entities.Permission> repository): IRequestHandler<CreatePermissionRequest, ApiResponse<PermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<PermissionDto>> Handle(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var permissionQuery = _repository.Query(p => p.Name == request.Permission.Name && !p.IsDeleted);
        var permission = await _repository.FirstOrDefaultAsync(permissionQuery, cancellationToken);
        if (permission is not null) return new ApiResponse<PermissionDto>("Permission already exists.");
                
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
