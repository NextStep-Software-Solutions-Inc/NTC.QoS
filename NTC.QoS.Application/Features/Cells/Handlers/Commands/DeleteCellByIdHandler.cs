using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class DeleteCellByIdHandler(IRepository<Cell> repository) : IRequestHandler<DeleteCellByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Cell> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellExists = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cellExists == null)
        {
            return new ApiResponse<bool>("Cell not found");
        }
        _repository.Delete(cellExists);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = "Cell deleted successfully"
        };
    }
}
