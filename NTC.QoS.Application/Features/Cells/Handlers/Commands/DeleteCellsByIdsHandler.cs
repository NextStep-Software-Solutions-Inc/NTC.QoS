using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class DeleteCellsByIdsHandler(ICellRepository repository) : IRequestHandler<DeleteCellsByIdsRequest, ApiResponse<bool>>
{
    private readonly ICellRepository _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteCellsByIdsRequest request, CancellationToken cancellationToken)
    {
        var cellsToDeleteQuery = _repository.Query(c => request.Ids.Contains(c.Id));
        var cellsToDelete = await _repository.ExecuteDeleteAsync(cellsToDeleteQuery, cancellationToken);

        if (cellsToDelete is 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified cell/s exist or are already deleted.",
            };
        }

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{cellsToDelete} cell/s deleted successfully."
        };
    }
}
