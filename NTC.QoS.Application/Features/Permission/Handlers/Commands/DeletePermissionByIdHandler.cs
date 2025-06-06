using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Permission.Handlers.Commands;

public class DeletePermissionByIdHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<DeletePermissionByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeletePermissionByIdRequest request, CancellationToken cancellationToken)
    {
        var permissionQuery = _repository.Query(p => p.Id == request.Id && !p.IsDeleted);
        var permission = await _repository.FirstOrDefaultAsync(permissionQuery, cancellationToken);

        if (permission is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                      {"Error", [ "Permission not found."] }
                },
                Message = "The permission you are trying to delete does not exist."
            };
        }

        _repository.Delete(permission);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = "Permission deleted successfully."
        };
    }
}
