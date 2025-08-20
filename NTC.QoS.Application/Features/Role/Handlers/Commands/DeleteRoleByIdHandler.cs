using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class DeleteRoleByIdHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<DeleteRoleByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteRoleByIdRequest request, CancellationToken cancellationToken)
    {
        var roleQuery = _repository.Query(r => r.Id == request.Id);
        var role = await _repository.FirstOrDefaultAsync(roleQuery, cancellationToken);

        if (role is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error", ["Role not found."] }
                },
                Message = "The role you are trying to delete does not exist."
            };
        }

        _repository.Delete(role);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = "Role deleted successfully."
        };
    }
}
