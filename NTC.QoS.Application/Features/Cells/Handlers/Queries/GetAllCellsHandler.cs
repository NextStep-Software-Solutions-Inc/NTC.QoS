
using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetAllCellsHandler(IRepository<Domain.Aggregates.Cell> repository) : IRequestHandler<GetAllCellsRequest, PaginatedResponse<Cell>>
{
    private readonly IRepository<Domain.Aggregates.Cell> _repository = repository;
    public async Task<PaginatedResponse<Cell>> Handle(GetAllCellsRequest request, CancellationToken cancellationToken)
    {
        var allCells = await _repository.ListAsync(c => c.IsDeleted != true,  cancellationToken);
        return new PaginatedResponse<Cell>(allCells, request.PageNumber, request.PageSize);
    }
}