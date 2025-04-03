using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class DeleteCellByIdHandler(ICellRepository repository) : IRequestHandler<DeleteCellByIdRequest, ApiResponse<bool>>
{
    private readonly ICellRepository _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cell = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Cell not found",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCell", ["Cell not found."]
                    }
                }
            };
        }

        _repository.Delete(cell);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = "Cell deleted successfully."
        };
    }
}
