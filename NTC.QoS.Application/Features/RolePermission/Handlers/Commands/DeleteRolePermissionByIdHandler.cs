using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Commands;

public class DeleteRolePermissionByIdHandler(IRolePermissionRepository rolePermissionRepository) : IRequestHandler<DeleteRolePermissionByIdRequest, ApiResponse<bool>>
{
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    public async Task<ApiResponse<bool>> Handle(DeleteRolePermissionByIdRequest request, CancellationToken cancellationToken)
    {
        var rolePermission = await _rolePermissionRepository.FindAsync(rp => rp.Id == request.Id && !rp.IsDeleted, cancellationToken);
        if (rolePermission is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error", [$"Role permission with ID [{request.Id}] not found."] }
                },
                Message = "Role permission does not exist."
            };
        }
        _rolePermissionRepository.Delete(rolePermission);
        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Message = "Role permission deleted successfully.",
            Succeeded = true
        };
    }
}
