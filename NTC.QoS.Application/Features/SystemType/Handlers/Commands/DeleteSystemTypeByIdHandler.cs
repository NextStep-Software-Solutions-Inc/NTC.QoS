using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.SystemType.Handlers.Commands;

public class DeleteSystemTypeByIdHandler(IRepository<Domain.Entities.SystemType> repository) : IRequestHandler<DeleteSystemTypeByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.SystemType> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteSystemTypeByIdRequest request, CancellationToken cancellationToken)
    {
        var systemTypeQuery = _repository.Query(st => st.Id == request.Id);
        var systemType = await _repository.FirstOrDefaultAsync(systemTypeQuery, cancellationToken);
        if (systemType is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullSystemType", ["System Type not found."]
                    }
                },
                Message = "The system type you are trying to delete does not exist."
            };
        }

        _repository.Delete(systemType);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "System Type deleted successfully."
        };
    }
}
