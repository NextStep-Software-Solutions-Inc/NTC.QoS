using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellByIdHandler(IRepository<Cell> cellRepository) : IRequestHandler<GetCellByIdRequest, ApiResponse<Cell>>
{
    private readonly IRepository<Cell> _cellRepository = cellRepository;
    public async Task<ApiResponse<Cell>> Handle(GetCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cell = await _cellRepository.GetByIdAsync(request.Id, cancellationToken);
        if (cell == null)
        {
            return new ApiResponse<Cell> { Succeeded = false, Message = "Cell not found" };
        }
        return new ApiResponse<Cell> 
        { 
            Succeeded = true, 
            Data = cell,
            Message = "Cell found",
            Error = new[] { "Error message" }
        };
    }
}
