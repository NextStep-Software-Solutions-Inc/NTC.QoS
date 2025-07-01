using MediatR;
using NTC.QoS.Application.Features.MIMO.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.MIMO.Handlers.Commands;

public class DeleteMIMOByIdHandler(IRepository<Domain.Entities.MIMO> repository) : IRequestHandler<DeleteMIMOByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.MIMO> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteMIMOByIdRequest request,
            CancellationToken cancellationToken)
    {
        var mimoQuery = _repository.Query(m => m.Id == request.Id);
        var mimo = await _repository.FirstOrDefaultAsync(mimoQuery, cancellationToken);

        if (mimo is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Mimo",  ["Mimo not found."] }
                },
                Message = "The mimo you are trying to delete does not exist."
            };
        }

        _repository.Delete(mimo);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"MIMO '{mimo.Type}' has been successfully deleted."
        };
    }
}
