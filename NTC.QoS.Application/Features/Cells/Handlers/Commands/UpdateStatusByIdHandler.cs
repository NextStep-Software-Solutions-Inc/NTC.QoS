using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class UpdateStatusByIdHandler(IRepository<Cell> repository) : IRequestHandler<UpdateCellStatusByIdRequest, ApiResponse<CellDto>>
{
    private readonly IRepository<Cell> _repository = repository;
    public async Task<ApiResponse<CellDto>> Handle(UpdateCellStatusByIdRequest request, CancellationToken cancellationToken)
    {
        var cellExists = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cellExists == null)
        {
            return new ApiResponse<CellDto>("Cell not found");
        }
        cellExists.Status = EnumHelper<CellStatusEnum>.GetValue( request.UpdateCellStatusDto.Status);
        _repository.Update(cellExists);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<CellDto>
        {
            Succeeded = true,
            Message = "Cell status updated successfully"
        };
    }
}
