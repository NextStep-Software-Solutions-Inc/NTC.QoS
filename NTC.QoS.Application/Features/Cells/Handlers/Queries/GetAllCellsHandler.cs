
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
        var allCells = await _repository.ListAsync(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(request.Search))
        {
            allCells = allCells.Where(cell => cell.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        var cell = allCells.Select(cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            Description = cell.Description ?? "",
            FileId = cell.FileId,
            File = new()
            {
                Name = cell.File.Name,
                Path = cell.File.Path
            },
            ServiceProviderId = cell.ServiceProviderId,
            ServiceProvider = new()
            {
                Id = cell.ServiceProvider.Id
            },
            ZoneId = cell.ZoneId,
            Zone = new()
            {
                Id = cell.Zone.Id,
                Name = cell.Zone.Name
            },
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

        return new PaginatedResponse<CellDto>(cell, request.PageNumber, request.PageSize);
    }

}
