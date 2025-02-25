
using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetAllCellsHandler(IRepository<Domain.Aggregates.Cell> repository) : IRequestHandler<GetAllCellsRequest, PaginatedResponse<CellDto>>
{
    private readonly IRepository<Domain.Aggregates.Cell> _repository = repository;
    public async Task<PaginatedResponse<CellDto>> Handle(GetAllCellsRequest request, CancellationToken cancellationToken)
    {
        var allCells = await _repository.ListAsync(p => !p.IsDeleted, cancellationToken);

        var cells = allCells.Select(cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            Description = cell.Description ?? "",
            FileId = cell.FileId,
            ServiceProviderId = cell.ServiceProviderId,
            ZoneId = cell.ZoneId,
            CID = cell.CID,
            LAC = cell.LAC,
            TAC = cell.TAC,
            SiteId = cell.SiteId,
            TowerType = cell.TowerType,
            Elevation = cell.Elevation,
            CoorLatitude = cell.CoorLatitude,
            CoorLongitude = cell.CoorLongitude,
            IsVerified = cell.IsVerified,
            Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
        }).ToList();

        if (!string.IsNullOrEmpty(request.Search))
        {
            cells = cells.Where(cell => cell.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return new PaginatedResponse<CellDto>(cells, pageNumber: request.PageNumber, pageSize: request.PageSize);
    }
}
