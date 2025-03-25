using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

public class DeleteNAPByIdHandler(IRepository<Domain.Aggregates.NAP> repository) : IRequestHandler<DeleteNAPByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.NAP> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteNAPByIdRequest request, CancellationToken cancellationToken)
    {
        var NAPExists = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (NAPExists is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Error", ["NAP not found."] 
                    }
                },
                Message = "The NAP you are trying to delete does not exist."
            };
        }
        _repository.Delete(NAPExists);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = "NAP deleted successfully."
        };
    }
}
