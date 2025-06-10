using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class DeleteUserByIdHandler(IUserRepository repository) : IRequestHandler<DeleteUserByIdRequest, ApiResponse<bool>>
{
    private readonly IUserRepository _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteUserByIdRequest request, CancellationToken cancellationToken)
    {
        var userQuery = _repository.Query(u => u.Id == request.Id && !u.IsDeleted);
        var user = await _repository.FirstOrDefaultAsync(userQuery, cancellationToken);
        if (user is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Error", ["User not found."] 
                    }
                },
                Message = "The user you are trying to delete does not exist."
            };
        }

        _repository.Delete(user);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Message = "User deleted successfully.",
            Succeeded = true
        };
    }
}
