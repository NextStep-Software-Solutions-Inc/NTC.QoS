using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Cellular.Requests;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Commands;

public class DeleteCellularByIdHandler(IRepository<Domain.Entities.Cellular> repository) : IRequestHandler<DeleteCellularByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Cellular> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteCellularByIdRequest request,
        CancellationToken cancellationToken)
    {
        var cellular = await _repository.FindAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

        if (cellular is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Error = new[] { "Cellular record not found." },
                Message = "The cellular record you are trying to delete does not exist.",
            };
        }

        _repository.Delete(cellular);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Error = Array.Empty<string>(),
            Message = $"Cellular record has been successfully deleted."
        };
    }
}