
using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetAllCellsHandler(ICellRepository repository) : IRequestHandler<GetAllCellsRequest, PaginatedResponse<CellDto>>
{
    private readonly ICellRepository _repository = repository;
    public async Task<PaginatedResponse<CellDto>> Handle(GetAllCellsRequest request, CancellationToken cancellationToken)
    {
        var allCells = await _repository.ListAsync(c => !c.IsDeleted, cancellationToken);

        if (!string.IsNullOrEmpty(request.Search))
        {
            allCells = allCells.Where(cell => cell.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        var cells = allCells.Select(static cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            Description = cell.Description ?? "",
            FileId = cell.FileId,
            File = cell.File != null
                ? new FileDto
                {
                    Name = cell.File.Name ?? string.Empty,
                    Path = cell.File.Path ?? string.Empty
                }
                : null!,
            ServiceProviderId = cell.ServiceProviderId,
            ServiceProvider = cell.ServiceProvider != null
                ? new ServiceProviderDto
                {
                    Service = cell.ServiceProvider.Service != null ? new() { Name = cell.ServiceProvider.Service.Name ?? string.Empty } : null,
                    Provider = cell.ServiceProvider.Provider != null ? new() { Name = cell.ServiceProvider.Provider.Name ?? string.Empty } : null,
                }
                : null!,
            ZoneId = cell.ZoneId,
            Zone = cell.Zone != null
                ? new ZoneDto
                {
                    Id = cell.Zone.Id,
                    Name = cell.Zone.Name ?? string.Empty
                }: null!,
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


        return new PaginatedResponse<CellDto>(cells, request.PageNumber, request.PageSize);
    }

}
